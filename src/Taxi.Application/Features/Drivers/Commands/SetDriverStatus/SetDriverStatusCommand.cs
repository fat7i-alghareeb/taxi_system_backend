using MediatR;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Drivers;

namespace Taxi.Application.Features.Drivers.Commands.SetDriverStatus;

public record SetDriverStatusCommand(DriverStatus Status) : IRequest<Result<Success>>;
