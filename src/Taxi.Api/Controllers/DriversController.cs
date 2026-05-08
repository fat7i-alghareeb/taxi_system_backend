using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.Drivers.Commands.CreateDriver;
using Taxi.Application.Features.Drivers.Commands.DeleteDriver;
using Taxi.Application.Features.Drivers.Commands.UpdateDriver;
using Taxi.Application.Features.Drivers.Dtos;
using Taxi.Application.Features.Drivers.Queries.GetDriverById;
using Taxi.Application.Features.Drivers.Queries.GetDrivers;
using Taxi.Application.Features.Vehicles.Commands.AssignVehicle;
using Taxi.Contracts.Requests.Drivers;
using Taxi.Domain.Common.Results;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/drivers")]
[Authorize(Roles = "Admin")]
public class DriversController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<DriverDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieves all drivers.")]
    [EndpointName("GetDrivers")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetDrivers(CancellationToken ct)
    {
        var result = await sender.Send(new GetDriversQuery(), ct);

        return result.Match(
            Ok,
            Problem);
    }

    [HttpGet("{id:guid}", Name = "GetDriverById")]
    [ProducesResponseType(typeof(DriverDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieves a driver by ID.")]
    [EndpointName("GetDriverById")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetDriverById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetDriverByIdQuery(id), ct);

        return result.Match(
            Ok,
            Problem);
    }

    [HttpPost]
    [ProducesResponseType(typeof(DriverDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Creates a new driver.")]
    [EndpointName("CreateDriver")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> CreateDriver([FromBody] CreateDriverRequest request, CancellationToken ct)
    {
        var command = new CreateDriverCommand(
            request.Phone,
            request.NameEn,
            request.NameAr,
            request.LicenseNumber);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => CreatedAtRoute(
                routeName: "GetDriverById",
                routeValues: new { version = "1.0", id = response.Id },
                value: response),
            Problem);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DriverDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Updates an existing driver.")]
    [EndpointName("UpdateDriver")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDriverRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateDriverCommand(id, request.LicenseNumber), ct);

        return result.Match(
            Ok,
            Problem);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Removes a driver.")]
    [EndpointName("RemoveDriver")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteDriverCommand(id), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }

    [HttpPost("{id:guid}/vehicles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Assigns an active vehicle to a driver.")]
    [EndpointName("AssignVehicle")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> AssignVehicle(Guid id, [FromBody] Guid vehicleId, CancellationToken ct)
    {
        var result = await sender.Send(new AssignVehicleCommand(id, vehicleId), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}

