using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Commands.RemoveVehicle;

public record RemoveVehicleCommand(Guid Id) : IRequest<Result<Deleted>>;
