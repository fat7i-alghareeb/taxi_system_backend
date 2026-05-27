using Asp.Versioning;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Taxi.Application.Features.Drivers.Commands.ApproveDriver;
using Taxi.Application.Features.Drivers.Commands.AssignDriverVehicleType;
using Taxi.Application.Features.Drivers.Commands.CreateDriver;
using Taxi.Application.Features.Drivers.Commands.DeleteDriver;
using Taxi.Application.Features.Drivers.Commands.SetDriverStatus;
using Taxi.Application.Features.Drivers.Commands.SuspendDriver;
using Taxi.Application.Features.Drivers.Commands.UpdateCurrentDriverProfile;
using Taxi.Application.Features.Drivers.Commands.UpdateDriver;
using Taxi.Application.Features.Drivers.Commands.UpdateDriverLocation;
using Taxi.Application.Features.Drivers.Dtos;
using Taxi.Application.Features.Drivers.Queries.GetAllDriversWithStatus;
using Taxi.Application.Features.Drivers.Queries.GetCurrentDriverProfile;
using Taxi.Application.Features.Drivers.Queries.GetDriverById;
using Taxi.Application.Features.Drivers.Queries.GetDriverEarnings;
using Taxi.Application.Features.Drivers.Queries.GetDrivers;
using Taxi.Contracts.Requests.Drivers;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Drivers;

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
            request.Name,
            request.LicenseNumber,
            request.VehicleTypeId);

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
        var result = await sender.Send(new UpdateDriverCommand(id, request.LicenseNumber, request.VehicleTypeId), ct);

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

    [HttpPost("{id:guid}/vehicle-type")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Assigns the vehicle type a driver can operate.")]
    [EndpointName("AssignDriverVehicleType")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> AssignVehicleType(Guid id, [FromBody] Guid vehicleTypeId, CancellationToken ct)
    {
        var result = await sender.Send(new AssignDriverVehicleTypeCommand(id, vehicleTypeId), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }

    [HttpPost("me/status")]
    [Authorize(Roles = "Driver,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Driver sets their online/offline status.")]
    [EndpointName("SetDriverStatus")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> SetStatus([FromBody] DriverStatus status, CancellationToken ct)
    {
        var result = await sender.Send(new SetDriverStatusCommand(status), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("me/location")]
    [Authorize(Roles = "Driver,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Driver updates their current GPS coordinates (REST backup).")]
    [EndpointName("UpdateDriverLocation")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UpdateLocation([FromBody] DriverLocationRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateDriverLocationCommand(request.Latitude, request.Longitude), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpGet("me/earnings")]
    [Authorize(Roles = "Driver,Admin")]
    [ProducesResponseType(typeof(DriverEarningsDto), StatusCodes.Status200OK)]
    [EndpointSummary("Driver retrieves their performance and earnings.")]
    [EndpointName("GetDriverEarnings")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetEarnings(CancellationToken ct)
    {
        var result = await sender.Send(new GetDriverEarningsQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("me")]
    [Authorize(Roles = "Driver,Admin")]
    [ProducesResponseType(typeof(DriverCurrentProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Driver retrieves their own profile.")]
    [EndpointName("GetCurrentDriverProfile")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetCurrentDriverProfile(CancellationToken ct)
    {
        var result = await sender.Send(new GetCurrentDriverProfileQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPut("me")]
    [Authorize(Roles = "Driver,Admin")]
    [ProducesResponseType(typeof(DriverCurrentProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Driver updates their own profile (name, email).")]
    [EndpointName("UpdateCurrentDriverProfile")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UpdateCurrentDriverProfile(
        [FromBody] UpdateCurrentDriverProfileRequest request,
        CancellationToken ct)
    {
        var command = new UpdateCurrentDriverProfileCommand(request.Name, request.Email);
        var result = await sender.Send(command, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Admin approves driver KYC once all documents are verified.")]
    [EndpointName("ApproveDriver")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ApproveDriverCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/suspend")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Admin suspends a driver's account.")]
    [EndpointName("SuspendDriver")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new SuspendDriverCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpGet("status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<DriverWithStatusDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Admin retrieves status and GPS locations of all online drivers.")]
    [EndpointName("GetAllDriversWithStatusAdmin")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetDriversWithStatus(CancellationToken ct)
    {
        var result = await sender.Send(new GetAllDriversWithStatusQuery(), ct);
        return result.Match(Ok, Problem);
    }
}

public record DriverLocationRequest(double Latitude, double Longitude);

public record UpdateCurrentDriverProfileRequest(string Name, string? Email);