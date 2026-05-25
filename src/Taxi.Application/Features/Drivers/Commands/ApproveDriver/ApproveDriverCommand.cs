using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Commands.ApproveDriver;

public record ApproveDriverCommand(Guid DriverId) : IRequest<Result<Success>>;
