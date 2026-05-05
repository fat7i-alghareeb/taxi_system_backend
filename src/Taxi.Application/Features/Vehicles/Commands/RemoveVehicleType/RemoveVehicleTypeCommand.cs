using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Commands.RemoveVehicleType;

public record RemoveVehicleTypeCommand(Guid Id) : IRequest<Result<Deleted>>;
