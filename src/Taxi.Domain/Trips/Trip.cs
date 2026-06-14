using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips.Events;

namespace Taxi.Domain.Trips;

public sealed class Trip : AuditableEntity
{
    private readonly List<TripStop> _stops = [];

    private Trip() { }

    private Trip(
        Guid id,
        string referenceCode,
        Guid passengerId,
        Guid vehicleTypeId,
        Guid quoteId,
        IEnumerable<TripStop> stops,
        DateTimeOffset? scheduledAtUtc,
        string? passengerNote)
        : base(id)
    {
        PassengerId = passengerId;
        ReferenceCode = referenceCode;
        VehicleTypeId = vehicleTypeId;
        QuoteId = quoteId;
        ScheduledAtUtc = scheduledAtUtc;
        PassengerNote = NormalizePassengerNote(passengerNote);
        Status = TripStatus.AwaitingPayment;
        _stops.AddRange(stops);
    }

    public Guid PassengerId { get; private set; }
    public string ReferenceCode { get; private set; } = default!;
    public Guid? DriverId { get; private set; }
    public Guid VehicleTypeId { get; private set; }
    public TripStatus Status { get; private set; }
    public Guid QuoteId { get; private set; }
    public IReadOnlyCollection<TripStop> Stops => _stops.AsReadOnly();
    public string? PassengerNote { get; private set; }
    public DateTimeOffset? ScheduledAtUtc { get; private set; }
    public DateTimeOffset? AssignedAtUtc { get; private set; }
    public DateTimeOffset? ArrivedAtUtc { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    /// <summary>
    /// Set once the passenger has been sent the "driver on the way" reminder for a
    /// scheduled trip (15 minutes before <see cref="ScheduledAtUtc"/>). Used by the
    /// background activation service to avoid sending the reminder more than once.
    /// </summary>
    public DateTimeOffset? PreArrivalNotifiedAtUtc { get; private set; }

    /// <summary>Passenger's star rating (1-5) for a completed trip; null until rated.</summary>
    public int? PassengerRating { get; private set; }
    public string? RatingComment { get; private set; }

    public static Result<Trip> Request(
        Guid id,
        string referenceCode,
        Guid passengerId,
        PricingQuote quote,
        IEnumerable<TripStop> stops,
        DateTimeOffset? scheduledAtUtc = null,
        string? passengerNote = null)
    {
        if (quote.IsExpired())
        {
            return TripErrors.QuoteExpired;
        }

        if (!stops.Any() || stops.Count() < 2)
        {
            return TripErrors.InvalidStops;
        }

        var trip = new Trip(
            id,
            referenceCode,
            passengerId,
            quote.VehicleTypeId,
            quote.Id,
            stops,
            scheduledAtUtc,
            passengerNote);

        trip.AddDomainEvent(new TripRequested
        {
            TripId = trip.Id,
            VehicleTypeId = quote.VehicleTypeId,
            PassengerId = passengerId,
        });

        return trip;
    }

    public Result<Success> UpdatePassengerNote(string? passengerNote)
    {
        if (Status is TripStatus.InProgress
            or TripStatus.Completed
            or TripStatus.Cancelled
            or TripStatus.PaymentFailed
            or TripStatus.Refunded)
        {
            return TripErrors.CannotUpdatePassengerNote;
        }

        PassengerNote = NormalizePassengerNote(passengerNote);
        return Result.Success;
    }

    public Result<Success> AssignDriver(Guid driverId)
    {
        if (Status != TripStatus.PendingDriver && Status != TripStatus.Scheduled)
        {
            return TripErrors.InvalidStatus(Status);
        }

        DriverId = driverId;
        Status = TripStatus.DriverAssigned;
        AssignedAtUtc = DateTimeOffset.UtcNow;

        AddDomainEvent(new DriverAssigned
        {
            TripId = Id,
            DriverId = driverId,
            PassengerId = PassengerId,
        });

        return Result.Success;
    }

    public Result<Success> DriverEnRoute()
    {
        if (Status != TripStatus.DriverAssigned)
        {
            return TripErrors.InvalidStatus(Status);
        }

        Status = TripStatus.DriverEnRoute;

        AddDomainEvent(new DriverEnRoute
        {
            TripId = Id,
            DriverId = DriverId ?? Guid.Empty,
            PassengerId = PassengerId,
        });

        return Result.Success;
    }

    public Result<Success> DriverArrived()
    {
        if (Status != TripStatus.DriverEnRoute)
        {
            return TripErrors.InvalidStatus(Status);
        }

        Status = TripStatus.DriverArrived;
        ArrivedAtUtc = DateTimeOffset.UtcNow;

        AddDomainEvent(new DriverArrived
        {
            TripId = Id,
            DriverId = DriverId ?? Guid.Empty,
            PassengerId = PassengerId,
        });

        return Result.Success;
    }

    public Result<Success> Start()
    {
        if (Status != TripStatus.DriverArrived)
        {
            return TripErrors.InvalidStatus(Status);
        }

        Status = TripStatus.InProgress;
        StartedAtUtc = DateTimeOffset.UtcNow;

        AddDomainEvent(new TripStarted
        {
            TripId = Id,
            PassengerId = PassengerId,
        });

        return Result.Success;
    }

    public Result<Success> Complete()
    {
        if (Status != TripStatus.InProgress)
        {
            return TripErrors.InvalidStatus(Status);
        }

        // Reject completion while intermediate stops (sequence 0 = pickup is
        // implicit when the trip starts; the final stop is implicit on
        // complete) are still pending. Drivers must walk through each stop in
        // order via CompleteStop before finishing the trip.
        var lastSequence = _stops.Count == 0 ? -1 : _stops.Max(s => s.Sequence);
        if (_stops.Any(s => s.Sequence > 0 && s.Sequence < lastSequence && !s.IsCompleted))
        {
            return TripErrors.PendingStopsRemaining;
        }

        Status = TripStatus.Completed;
        CompletedAtUtc = DateTimeOffset.UtcNow;

        AddDomainEvent(new TripCompleted
        {
            TripId = Id,
            PassengerId = PassengerId,
            DriverId = DriverId,
        });

        return Result.Success;
    }

    /// <summary>
    /// Marks an intermediate stop as completed during an in-progress trip.
    /// Stops must be completed in order. The first stop (pickup, sequence 0)
    /// and the last stop (dropoff) are not completed via this method — pickup
    /// is implicit when the trip starts; dropoff is implicit in
    /// <see cref="Complete"/>.
    /// </summary>
    public Result<Success> CompleteStop(int sequence)
    {
        if (Status != TripStatus.InProgress)
        {
            return TripErrors.InvalidStatus(Status);
        }

        var stop = _stops.FirstOrDefault(s => s.Sequence == sequence);
        if (stop is null)
        {
            return TripErrors.StopNotFound(sequence);
        }

        if (stop.IsCompleted)
        {
            return TripErrors.StopAlreadyCompleted(sequence);
        }

        // Reject completing a stop while any earlier stop is still pending.
        if (_stops.Any(s => s.Sequence < sequence && !s.IsCompleted))
        {
            return TripErrors.StopOutOfOrder(sequence);
        }

        stop.MarkCompleted();

        AddDomainEvent(new TripStopCompleted
        {
            TripId = Id,
            PassengerId = PassengerId,
            DriverId = DriverId,
            Sequence = sequence,
        });

        return Result.Success;
    }

    public Result<Success> Cancel()
    {
        if (Status == TripStatus.Completed || Status == TripStatus.Cancelled || Status == TripStatus.Refunded)
        {
            return TripErrors.InvalidStatus(Status);
        }

        Status = TripStatus.Cancelled;

        AddDomainEvent(new TripCancelled
        {
            TripId = Id,
            PassengerId = PassengerId,
            DriverId = DriverId,
        });

        return Result.Success;
    }

    public Result<Success> ActivateScheduled()
    {
        if (Status != TripStatus.Scheduled)
        {
            return TripErrors.InvalidStatus(Status);
        }

        Status = TripStatus.PendingDriver;

        AddDomainEvent(new TripRequested
        {
            TripId = Id,
            VehicleTypeId = VehicleTypeId,
            PassengerId = PassengerId,
            WasScheduled = true,
        });

        return Result.Success;
    }

    public Result<Success> ConfirmPayment()
    {
        if (Status != TripStatus.AwaitingPayment)
        {
            return TripErrors.InvalidStatus(Status);
        }

        Status = ScheduledAtUtc.HasValue ? TripStatus.Scheduled : TripStatus.PendingDriver;

        AddDomainEvent(new PaymentConfirmed
        {
            TripId = Id,
            PassengerId = PassengerId,
            ScheduledAtUtc = ScheduledAtUtc,
        });

        return Result.Success;
    }

    public Result<Success> MarkPaymentFailed(string reason)
    {
        if (Status != TripStatus.AwaitingPayment)
        {
            return TripErrors.InvalidStatus(Status);
        }

        Status = TripStatus.PaymentFailed;

        AddDomainEvent(new PaymentFailed
        {
            TripId = Id,
            PassengerId = PassengerId,
            Reason = reason,
        });

        return Result.Success;
    }

    public Result<Success> MarkRefunded(decimal amount)
    {
        if (Status != TripStatus.Cancelled && Status != TripStatus.Completed)
        {
            return TripErrors.InvalidStatus(Status);
        }

        Status = TripStatus.Refunded;

        AddDomainEvent(new TripRefunded
        {
            TripId = Id,
            PassengerId = PassengerId,
            Amount = amount,
        });

        return Result.Success;
    }

    public Result<Success> SoftDelete()
    {
        DeletedAtUtc = DateTimeOffset.UtcNow;
        return Result.Success;
    }

    /// <summary>
    /// Records that the pre-arrival ("driver on the way") reminder has been sent for a
    /// scheduled trip, so the activation service does not send it again.
    /// </summary>
    public void MarkPreArrivalNotified()
    {
        PreArrivalNotifiedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Stores the passenger's 1-5 star rating for a completed trip.</summary>
    public Result<Success> Rate(int stars, string? comment)
    {
        if (Status != TripStatus.Completed)
        {
            return TripErrors.InvalidStatus(Status);
        }

        if (stars is < 1 or > 5)
        {
            return TripErrors.InvalidRating;
        }

        PassengerRating = stars;
        RatingComment = NormalizePassengerNote(comment);
        return Result.Success;
    }

    private static string? NormalizePassengerNote(string? passengerNote)
    {
        var normalized = passengerNote?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}

