using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Commands.SuspendDriver;

public record SuspendDriverCommand(Guid DriverId) : IRequest<Result<Success>>;
