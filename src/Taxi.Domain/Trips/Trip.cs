using System.Text.RegularExpressions;
using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips.Events;

namespace Taxi.Domain.Trips;

public sealed class Trip : AuditableEntity
{
    private static readonly TimeSpan ScheduledEnRouteLeadTime = TimeSpan.FromMinutes(15);

    // Tolerance that absorbs small clock differences between the driver's device
    // and the server when checking whether a scheduled trip's start time has
    // arrived. Without it, a few seconds of skew rejects "Start" right at the
    // boundary with a confusing "trip time not come yet" error.
    private static readonly TimeSpan ScheduledStartSkew = TimeSpan.FromMinutes(1);
    private static readonly Regex FlightNumberPattern = new(
        @"^[A-Z0-9](?:[A-Z0-9 -]{0,13}[A-Z0-9])?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

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
        string? passengerNote,
        bool isAirport,
        string? flightNumber)
        : base(id)
    {
        PassengerId = passengerId;
        ReferenceCode = referenceCode;
        VehicleTypeId = vehicleTypeId;
        QuoteId = quoteId;
        ScheduledAtUtc = scheduledAtUtc;
        PassengerNote = NormalizePassengerNote(passengerNote);
        IsAirport = isAirport;
        FlightNumber = isAirport ? NormalizeFlightNumber(flightNumber) : null;
        Status = TripStatus.AwaitingPayment;
        _stops.AddRange(stops);
    }

    public Guid PassengerId { get; private set; }
    public string ReferenceCode { get; private set; } = default!;
    public Guid? DriverId { get; private set; }
    public Guid? AcceptedByAdminId { get; private set; }
    public Guid VehicleTypeId { get; private set; }
    public TripStatus Status { get; private set; }
    public Guid QuoteId { get; private set; }
    public IReadOnlyCollection<TripStop> Stops => _stops.AsReadOnly();
    public string? PassengerNote { get; private set; }

    /// <summary>
    /// Airport trip flagged by the passenger at booking. Airport trips get a longer
    /// free waiting window (30 min vs 10) and the driver may decline to keep waiting
    /// after it, which cancels the trip with a 20% passenger refund.
    /// </summary>
    public bool IsAirport { get; private set; }
    public string? FlightNumber { get; private set; }
    public DateTimeOffset? ScheduledAtUtc { get; private set; }
    public DateTimeOffset? AssignedAtUtc { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public DateTimeOffset? ArrivedAtUtc { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public DateTimeOffset? UnacceptedReminder60SentAtUtc { get; private set; }
    public DateTimeOffset? UnacceptedReminder30SentAtUtc { get; private set; }
    public DateTimeOffset? UnacceptedReminder15SentAtUtc { get; private set; }
    public DateTimeOffset? UnacceptedOverdueSentAtUtc { get; private set; }
    public DateTimeOffset? AcceptedReminder30SentAtUtc { get; private set; }
    public DateTimeOffset? AcceptedReminder15SentAtUtc { get; private set; }

    public int PassengerCount { get; private set; } = 1;
    public int BagCount { get; private set; } = 0;

    /// <summary>Passenger's star rating (1-5) for a completed trip; null until rated.</summary>
    public int? PassengerRating { get; private set; }
    public string? RatingComment { get; private set; }

    private bool IsWithinEditWindow => DateTimeOffset.UtcNow <= CreatedAtUtc.AddHours(1);

    public static Result<Trip> Request(
        Guid id,
        string referenceCode,
        Guid passengerId,
        PricingQuote quote,
        IEnumerable<TripStop> stops,
        DateTimeOffset? scheduledAtUtc = null,
        string? passengerNote = null,
        bool isAirport = false,
        string? flightNumber = null)
    {
        if (quote.IsExpired())
        {
            return TripErrors.QuoteExpired;
        }

        if (!stops.Any() || stops.Count() < 2)
        {
            return TripErrors.InvalidStops;
        }

        var normalizedFlightNumber = NormalizeFlightNumber(flightNumber);
        if (isAirport && normalizedFlightNumber is null)
        {
            return TripErrors.FlightNumberRequired;
        }

        if (isAirport && !FlightNumberPattern.IsMatch(normalizedFlightNumber!))
        {
            return TripErrors.InvalidFlightNumber;
        }

        var trip = new Trip(
            id,
            referenceCode,
            passengerId,
            quote.VehicleTypeId,
            quote.Id,
            stops,
            scheduledAtUtc,
            passengerNote,
            isAirport,
            normalizedFlightNumber);

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
        if (Status != TripStatus.AwaitingAdminAcceptance)
        {
            return TripErrors.InvalidStatus(Status);
        }

        DriverId = driverId;
        Status = TripStatus.Accepted;
        AssignedAtUtc = DateTimeOffset.UtcNow;

        AddDomainEvent(new DriverAssigned
        {
            TripId = Id,
            DriverId = driverId,
            PassengerId = PassengerId,
        });

        return Result.Success;
    }

    public Result<Success> AcceptByAdmin(Guid adminId, DateTimeOffset acceptedAtUtc)
    {
        if (Status != TripStatus.AwaitingAdminAcceptance || AcceptedByAdminId.HasValue)
        {
            return TripErrors.AlreadyAccepted;
        }

        AcceptedByAdminId = adminId;
        AcceptedAtUtc = acceptedAtUtc;
        Status = TripStatus.Accepted;

        AddDomainEvent(new AdminAcceptedTrip
        {
            TripId = Id,
            PassengerId = PassengerId,
            AdminId = adminId,
            ScheduledAtUtc = ScheduledAtUtc,
        });

        return Result.Success;
    }

    public Result<Success> DriverEnRoute(DateTimeOffset now, bool forceOverride = false)
    {
        if (Status != TripStatus.Accepted)
        {
            return TripErrors.InvalidStatus(Status);
        }

        if (!forceOverride &&
            ScheduledAtUtc.HasValue &&
            ScheduledAtUtc.Value > now.Add(ScheduledEnRouteLeadTime))
        {
            return TripErrors.ScheduledEnRouteNotReady;
        }

        Status = TripStatus.EnRoute;

        AddDomainEvent(new DriverEnRoute
        {
            TripId = Id,
            DriverId = DriverId ?? AcceptedByAdminId ?? Guid.Empty,
            PassengerId = PassengerId,
        });

        return Result.Success;
    }

    public Result<Success> DriverArrived(DateTimeOffset now)
    {
        if (Status != TripStatus.EnRoute)
        {
            return TripErrors.InvalidStatus(Status);
        }

        // Scheduled trips may be marked arrived early: the driver can signal they
        // are waiting at the pickup before the booked time. The free waiting
        // window still starts at the scheduled time (see ArriveTripCommandHandler),
        // and the trip cannot be started until then (see Start below).
        var isEarlyArrival = ScheduledAtUtc.HasValue && ScheduledAtUtc.Value > now;

        Status = TripStatus.Arrived;
        ArrivedAtUtc = now;

        AddDomainEvent(new DriverArrived
        {
            TripId = Id,
            DriverId = DriverId ?? AcceptedByAdminId ?? Guid.Empty,
            PassengerId = PassengerId,
            IsEarlyArrival = isEarlyArrival,
        });

        return Result.Success;
    }

    public Result<Success> Start(DateTimeOffset now, bool forceOverride = false)
    {
        if (Status != TripStatus.Arrived)
        {
            return TripErrors.InvalidStatus(Status);
        }

        if (!forceOverride &&
            ScheduledAtUtc.HasValue &&
            ScheduledAtUtc.Value > now.Add(ScheduledStartSkew))
        {
            return TripErrors.ScheduledStartNotReady;
        }

        Status = TripStatus.InProgress;
        StartedAtUtc = now;

        AddDomainEvent(new TripStarted
        {
            TripId = Id,
            PassengerId = PassengerId,
        });

        return Result.Success;
    }

    public Result<Success> Complete(DateTimeOffset now)
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
        CompletedAtUtc = now;

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

    public Result<Success> ConfirmPayment()
    {
        if (Status != TripStatus.AwaitingPayment)
        {
            return TripErrors.InvalidStatus(Status);
        }

        Status = TripStatus.AwaitingAdminAcceptance;

        AddDomainEvent(new PaymentConfirmed
        {
            TripId = Id,
            PassengerId = PassengerId,
            VehicleTypeId = VehicleTypeId,
            ReferenceCode = ReferenceCode,
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

    public DateTimeOffset? DispatchWindowOpensAtUtc =>
        ScheduledAtUtc?.Subtract(ScheduledEnRouteLeadTime);

    public bool CanMarkEnRoute(DateTimeOffset now) =>
        Status == TripStatus.Accepted &&
        (!ScheduledAtUtc.HasValue || now >= DispatchWindowOpensAtUtc!.Value);

    public TripAttentionState GetAttentionState(DateTimeOffset now)
    {
        if (!ScheduledAtUtc.HasValue ||
            Status is TripStatus.EnRoute
                or TripStatus.Arrived
                or TripStatus.InProgress
                or TripStatus.Completed
                or TripStatus.Cancelled
                or TripStatus.PaymentFailed
                or TripStatus.Refunded)
        {
            return TripAttentionState.Normal;
        }

        var remaining = ScheduledAtUtc.Value - now;
        if (remaining <= TimeSpan.Zero)
        {
            return TripAttentionState.Overdue;
        }

        if (remaining <= TimeSpan.FromMinutes(15))
        {
            return TripAttentionState.Urgent;
        }

        var dueSoonThreshold = Status == TripStatus.Accepted
            ? TimeSpan.FromMinutes(30)
            : TimeSpan.FromMinutes(60);
        return remaining <= dueSoonThreshold
            ? TripAttentionState.DueSoon
            : TripAttentionState.Normal;
    }

    public bool HasReminderBeenSent(ScheduledTripReminderStage stage) => stage switch
    {
        ScheduledTripReminderStage.Unaccepted60Minutes => UnacceptedReminder60SentAtUtc.HasValue,
        ScheduledTripReminderStage.Unaccepted30Minutes => UnacceptedReminder30SentAtUtc.HasValue,
        ScheduledTripReminderStage.Unaccepted15Minutes => UnacceptedReminder15SentAtUtc.HasValue,
        ScheduledTripReminderStage.UnacceptedOverdue => UnacceptedOverdueSentAtUtc.HasValue,
        ScheduledTripReminderStage.Accepted30Minutes => AcceptedReminder30SentAtUtc.HasValue,
        ScheduledTripReminderStage.Accepted15Minutes => AcceptedReminder15SentAtUtc.HasValue,
        _ => false,
    };

    public void MarkReminderSent(ScheduledTripReminderStage stage, DateTimeOffset sentAtUtc)
    {
        switch (stage)
        {
            case ScheduledTripReminderStage.Unaccepted60Minutes:
                UnacceptedReminder60SentAtUtc = sentAtUtc;
                break;
            case ScheduledTripReminderStage.Unaccepted30Minutes:
                UnacceptedReminder30SentAtUtc = sentAtUtc;
                break;
            case ScheduledTripReminderStage.Unaccepted15Minutes:
                UnacceptedReminder15SentAtUtc = sentAtUtc;
                break;
            case ScheduledTripReminderStage.UnacceptedOverdue:
                UnacceptedOverdueSentAtUtc = sentAtUtc;
                break;
            case ScheduledTripReminderStage.Accepted30Minutes:
                AcceptedReminder30SentAtUtc = sentAtUtc;
                break;
            case ScheduledTripReminderStage.Accepted15Minutes:
                AcceptedReminder15SentAtUtc = sentAtUtc;
                break;
        }

        AddDomainEvent(new ScheduledTripAdminReminder
        {
            TripId = Id,
            ReferenceCode = ReferenceCode,
            ScheduledAtUtc = ScheduledAtUtc!.Value,
            Stage = stage,
        });
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

    public Result<Success> UpdateScheduledTime(DateTimeOffset? newScheduledAtUtc)
    {
        if (!IsWithinEditWindow)
        {
            return TripErrors.EditWindowExpired;
        }

        if (Status is not (TripStatus.AwaitingAdminAcceptance or TripStatus.Accepted))
        {
            return TripErrors.InvalidStatus(Status);
        }

        ScheduledAtUtc = newScheduledAtUtc;
        return Result.Success;
    }

    public Result<Success> UpdateStops(IReadOnlyList<TripStop> newStops)
    {
        if (!IsWithinEditWindow)
        {
            return TripErrors.EditWindowExpired;
        }

        if (Status is not (TripStatus.AwaitingAdminAcceptance or TripStatus.Accepted))
        {
            return TripErrors.InvalidStatus(Status);
        }

        if (newStops.Count < 2)
        {
            return TripErrors.InvalidStops;
        }

        _stops.Clear();
        _stops.AddRange(newStops);
        return Result.Success;
    }

    public Result<Success> UpdatePassengerCount(int count, Guid? newVehicleTypeId = null)
    {
        if (!IsWithinEditWindow)
        {
            return TripErrors.EditWindowExpired;
        }

        if (Status is not (TripStatus.AwaitingAdminAcceptance or TripStatus.Accepted))
        {
            return TripErrors.InvalidStatus(Status);
        }

        if (count < 1)
        {
            return TripErrors.InvalidPassengerCount;
        }

        PassengerCount = count;

        if (newVehicleTypeId.HasValue)
        {
            VehicleTypeId = newVehicleTypeId.Value;
        }

        return Result.Success;
    }

    public Result<Success> UpdateBagCount(int count)
    {
        if (!IsWithinEditWindow)
        {
            return TripErrors.EditWindowExpired;
        }

        if (Status is not (TripStatus.AwaitingAdminAcceptance or TripStatus.Accepted))
        {
            return TripErrors.InvalidStatus(Status);
        }

        if (count < 0)
        {
            return TripErrors.InvalidBagCount;
        }

        BagCount = count;
        return Result.Success;
    }

    public void UpdateQuote(Guid newQuoteId, Guid? newVehicleTypeId = null)
    {
        QuoteId = newQuoteId;
        if (newVehicleTypeId.HasValue)
        {
            VehicleTypeId = newVehicleTypeId.Value;
        }
    }

    private static string? NormalizePassengerNote(string? passengerNote)
    {
        var normalized = passengerNote?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? NormalizeFlightNumber(string? flightNumber)
    {
        if (string.IsNullOrWhiteSpace(flightNumber))
        {
            return null;
        }

        var collapsed = Regex.Replace(flightNumber.Trim(), @"\s+", " ");
        return collapsed.ToUpperInvariant();
    }
}

