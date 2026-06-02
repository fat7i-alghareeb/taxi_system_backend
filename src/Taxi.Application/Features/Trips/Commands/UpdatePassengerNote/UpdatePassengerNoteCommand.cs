using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.UpdatePassengerNote;

public record UpdatePassengerNoteCommand(
    Guid TripId,
    string? PassengerNote) : IRequest<Result<TripDto>>;
