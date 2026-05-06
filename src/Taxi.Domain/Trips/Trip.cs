using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

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
        IEnumerable<TripStop> stops)
        : base(id)
    {
        PassengerId = passengerId;
        ReferenceCode = referenceCode;
        VehicleTypeId = vehicleTypeId;
        QuoteId = quoteId;
        Status = TripStatus.PendingDriver;
        _stops.AddRange(stops);
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid PassengerId { get; private set; }
    public string ReferenceCode { get; private set; } = default!;
    public Guid? DriverId { get; private set; }
    public Guid VehicleTypeId { get; private set; }
    public TripStatus Status { get; private set; }
    public Guid QuoteId { get; private set; }
    public IReadOnlyCollection<TripStop> Stops => _stops.AsReadOnly();
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static Result<Trip> Request(
        Guid id,
        Guid passengerId,
        Guid vehicleTypeId,
        PricingQuote quote,
        IEnumerable<TripStop> stops)
    {
        if (quote.IsExpired())
        {
            return TripErrors.QuoteExpired;
        }

        if (!stops.Any() || stops.Count() < 2)
        {
            return TripErrors.InvalidStops;
        }

        var referenceCode = $"TRP-{new Random().Next(1000, 9999)}";

        return new Trip(id, referenceCode, passengerId, vehicleTypeId, quote.Id, stops);
    }

    public Result<Success> AssignDriver(Guid driverId)
    {
        if (Status != TripStatus.PendingDriver)
        {
            return TripErrors.InvalidStatus(Status);
        }

        DriverId = driverId;
        Status = TripStatus.DriverAssigned;
        return Result.Success;
    }

    public Result<Success> Start()
    {
        if (Status != TripStatus.DriverAssigned)
        {
            return TripErrors.InvalidStatus(Status);
        }

        Status = TripStatus.InProgress;
        StartedAtUtc = DateTimeOffset.UtcNow;
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
        return Result.Success;
    }

    public Result<Success> Cancel()
    {
        if (Status == TripStatus.Completed || Status == TripStatus.Cancelled)
        {
            return TripErrors.InvalidStatus(Status);
        }

        Status = TripStatus.Cancelled;
        return Result.Success;
    }

    public void SoftDelete() => DeletedAtUtc = DateTimeOffset.UtcNow;
}
