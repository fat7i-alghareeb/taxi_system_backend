using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Commands.UpdateDriverLocation;

public record UpdateDriverLocationCommand(double Latitude, double Longitude) : IRequest<Result<Success>>;
