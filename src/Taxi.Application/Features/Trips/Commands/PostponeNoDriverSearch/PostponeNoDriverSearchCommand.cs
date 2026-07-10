using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.PostponeNoDriverSearch;

/// <summary>
/// Snoozes the "no driver found" prompt: keeps the trip pending (no reschedule,
/// no refund) and re-arms the prompt 40 minutes out.
/// </summary>
public record PostponeNoDriverSearchCommand(Guid TripId) : IRequest<Result<TripDto>>;
