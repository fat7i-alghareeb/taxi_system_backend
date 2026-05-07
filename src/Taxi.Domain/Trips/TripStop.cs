using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Trips;

public sealed class TripStop
{
    private TripStop() { }

    private TripStop(Coordinate coordinate, string address, int sequence)
    {
        Coordinate = coordinate;
        Address = address;
        Sequence = sequence;
    }

    public Coordinate Coordinate { get; private set; } = default!;
    public string Address { get; private set; } = default!;
    public int Sequence { get; private set; }

    public static Result<TripStop> Create(Coordinate coordinate, string address, int sequence)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return Error.Validation(LocalizationKeys.Validation.RequiredField, "Stop address is required.");
        }

        if (sequence < 0)
        {
            return Error.Validation(LocalizationKeys.Validation.InvalidFormat, "Stop sequence must be zero or greater.");
        }

        return new TripStop(coordinate, address, sequence);
    }
}
