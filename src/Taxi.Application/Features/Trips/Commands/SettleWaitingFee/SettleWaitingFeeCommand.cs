using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.SettleWaitingFee;

public record SettleWaitingFeeCommand(Guid TripId) : IRequest<Result<WaitingFeeSettlementDto>>;
