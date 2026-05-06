using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Commands.DeleteDriver;

public record DeleteDriverCommand(Guid Id) : IRequest<Result<Deleted>>;
