using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Trips;

public sealed class TripStop
{
    private TripStop() { }

    private TripStop(Coordinate coordinate, int sequence)
    {
        Coordinate = coordinate;
        Sequence = sequence;
    }

    public Coordinate Coordinate { get; private set; } = default!;
    public int Sequence { get; private set; }

    public static Result<TripStop> Create(Coordinate coordinate, int sequence)
    {
        if (sequence < 0)
        {
            return Error.Validation(LocalizationKeys.Validation.InvalidFormat, "Stop sequence must be zero or greater.");
        }

        return new TripStop(coordinate, sequence);
    }
}
