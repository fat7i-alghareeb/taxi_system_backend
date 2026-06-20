using MediatR;

using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetTripMessages;

/// <summary>Loads the chat history for a trip, oldest first.</summary>
public record GetTripMessagesQuery(Guid TripId)
    : IRequest<Result<IReadOnlyList<TripMessageDto>>>;
