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
        DateTimeOffset? scheduledAtUtc)
        : base(id)
    {
        PassengerId = passengerId;
        ReferenceCode = referenceCode;
        VehicleTypeId = vehicleTypeId;
        QuoteId = quoteId;
        ScheduledAtUtc = scheduledAtUtc;
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
    public DateTimeOffset? ScheduledAtUtc { get; private set; }
    public DateTimeOffset? AssignedAtUtc { get; private set; }
    public DateTimeOffset? ArrivedAtUtc { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static Result<Trip> Request(
        Guid id,
        string referenceCode,
        Guid passengerId,
        PricingQuote quote,
        IEnumerable<TripStop> stops,
        DateTimeOffset? scheduledAtUtc = null)
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
            scheduledAtUtc);

        trip.AddDomainEvent(new TripRequested
        {
            TripId = trip.Id,
            VehicleTypeId = quote.VehicleTypeId,
            PassengerId = passengerId,
        });

        return trip;
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

        Status = ScheduledAtUtc.HasValue ? TripStatus.Scheduled : TripStatus.PendingDriver;

        AddDomainEvent(new PaymentConfirmed
        {
            TripId = Id,
            PassengerId = PassengerId,
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
}

