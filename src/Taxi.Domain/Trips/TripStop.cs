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
        return new TripStop(coordinate, address, sequence);
    }
}
