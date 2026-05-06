using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Taxi.Application.Features.Vehicles.Commands.CreateVehicleType;
using Taxi.Application.Features.Vehicles.Commands.RemoveVehicleType;
using Taxi.Application.Features.Vehicles.Commands.UpdateVehicleType;
using Taxi.Application.Features.Vehicles.Dtos;
using Taxi.Application.Features.Vehicles.Queries.GetVehicleCatalog;
using Taxi.Application.Features.Vehicles.Queries.GetVehicleTypeByCode;
using Taxi.Application.Features.Vehicles.Queries.GetVehicleTypeById;
using Taxi.Contracts.Requests.Vehicles;
using Taxi.Domain.Common.Results;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/vehicle-types")]
public class VehicleTypesController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<VehicleTypeDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieves the catalog of vehicle types.")]
    [EndpointDescription("Returns all active vehicle types with their localized names and pricing.")]
    [EndpointName("GetVehicleCatalog")]
    [MapToApiVersion("1.0")]
    [OutputCache(Duration = 60)]
    public async Task<IActionResult> GetCatalog(CancellationToken ct)
    {
        var result = await sender.Send(new GetVehicleCatalogQuery(), ct);

        return result.Match(
            Ok,
            Problem);
    }

    [HttpGet("{id:guid}", Name = "GetVehicleTypeById")]
    [ProducesResponseType(typeof(VehicleTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieves a vehicle type by ID.")]
    [EndpointName("GetVehicleTypeById")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetVehicleTypeByIdQuery(id), ct);

        return result.Match(
            Ok,
            Problem);
    }

    [HttpGet("code/{code}")]
    [ProducesResponseType(typeof(VehicleTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieves a vehicle type by its unique business code.")]
    [EndpointName("GetVehicleTypeByCode")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken ct)
    {
        var result = await sender.Send(new GetVehicleTypeByCodeQuery(code), ct);

        return result.Match(
            Ok,
            Problem);
    }

    [HttpPost]
    [ProducesResponseType(typeof(VehicleTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Creates a new vehicle type.")]
    [EndpointName("CreateVehicleType")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Create([FromBody] CreateVehicleTypeRequest request, CancellationToken ct)
    {
        var command = new CreateVehicleTypeCommand(
            request.Code,
            request.NameEn,
            request.NameAr,
            request.NameNl,
            request.Capacity,
            request.RatePerKm,
            request.RatePerMin,
            request.MinFare,
            request.Currency,
            request.SortOrder);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => CreatedAtRoute(
                routeName: "GetVehicleTypeById",
                routeValues: new { version = "1.0", id = response.Id },
                value: response),
            Problem);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(VehicleTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Updates an existing vehicle type.")]
    [EndpointName("UpdateVehicleType")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleTypeRequest request, CancellationToken ct)
    {
        var command = new UpdateVehicleTypeCommand(
            id,
            request.RatePerKm,
            request.RatePerMin,
            request.MinFare,
            request.IsActive);

        var result = await sender.Send(command, ct);

        return result.Match(
            Ok,
            Problem);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Removes a vehicle type.")]
    [EndpointName("RemoveVehicleType")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new RemoveVehicleTypeCommand(id), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}
