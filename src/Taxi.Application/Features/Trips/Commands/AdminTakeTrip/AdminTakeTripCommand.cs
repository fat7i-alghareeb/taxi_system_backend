using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.AdminTakeTrip;

/// <summary>
/// Lets an authenticated admin take ownership of a trip and act as its driver.
/// The handler ensures the admin has a backing Driver record (creating one on first use)
/// and assigns the trip to that record.
/// </summary>
public record AdminTakeTripCommand(Guid TripId) : IRequest<Result<Success>>;
