using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.Vehicles.Commands.CreateVehicle;
using Taxi.Application.Features.Vehicles.Commands.RemoveVehicle;
using Taxi.Application.Features.Vehicles.Commands.UpdateVehicle;
using Taxi.Application.Features.Vehicles.Queries.GetVehicleById;
using Taxi.Application.Features.Vehicles.Queries.GetVehicles;
using Taxi.Contracts.Requests.Vehicles;
using Taxi.Contracts.Responses.Vehicles;
using Taxi.Domain.Common.Results;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/vehicles")]
[Authorize(Roles = "Admin")]
public class VehiclesController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<VehicleDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieves a list of all vehicles.")]
    [EndpointName("GetVehicles")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await sender.Send(new GetVehiclesQuery(), ct);

        return result.Match(
            Ok,
            Problem);
    }

    [HttpGet("{id:guid}", Name = "GetVehicleById")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieves a vehicle by ID.")]
    [EndpointName("GetVehicleById")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetVehicleByIdQuery(id), ct);

        return result.Match(
            Ok,
            Problem);
    }

    [HttpPost]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Creates a new vehicle.")]
    [EndpointName("CreateVehicle")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Create([FromBody] CreateVehicleRequest request, CancellationToken ct)
    {
        var command = new CreateVehicleCommand(
            request.VehicleTypeId,
            request.DriverId,
            request.Make,
            request.Model,
            request.Year,
            request.Color,
            request.LicensePlate);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => CreatedAtRoute(
                routeName: "GetVehicleById",
                routeValues: new { version = "1.0", id = response.Id },
                value: response),
            Problem);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Updates an existing vehicle.")]
    [EndpointName("UpdateVehicle")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleRequest request, CancellationToken ct)
    {
        var command = new UpdateVehicleCommand(
            id,
            request.Color,
            request.LicensePlate,
            request.IsActive);

        var result = await sender.Send(command, ct);

        return result.Match(
            Ok,
            Problem);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Removes a vehicle.")]
    [EndpointName("RemoveVehicle")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new RemoveVehicleCommand(id), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}

