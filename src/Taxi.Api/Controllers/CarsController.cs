using Asp.Versioning;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

using Taxi.Application.Features.Cars.Commands.CreateCar;
using Taxi.Application.Features.Cars.Commands.RemoveCar;
using Taxi.Application.Features.Cars.Commands.UpdateCar;
using Taxi.Application.Features.Cars.Dtos;
using Taxi.Application.Features.Cars.Queries.GetCarById;
using Taxi.Application.Features.Cars.Queries.GetCars;
using Taxi.Contracts.Requests.Cars;

namespace Taxi.Api.Controllers;

[Route("api/v{version:apiVersion}/cars")]
[ApiVersion("1.0")]
[Authorize]
public sealed class CarsController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<CarDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves a list of cars.")]
    [EndpointDescription("Returns all cars in the system.")]
    [EndpointName("GetCars")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    [OutputCache(Duration = 60)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await sender.Send(new GetCarsQuery(), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpGet("{carId:guid}", Name = "GetCarById")]
    [ProducesResponseType(typeof(CarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves a car by ID.")]
    [EndpointDescription("Returns detailed information about the specified car if found.")]
    [EndpointName("GetCarById")]
    [MapToApiVersion("1.0")]
    [OutputCache(Duration = 60)]
    public async Task<IActionResult> GetById(Guid carId, CancellationToken ct)
    {
        var result = await sender.Send(new GetCarByIdQuery(carId), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CarDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Creates a new car.")]
    [EndpointDescription("Adds a new car to the system.")]
    [EndpointName("CreateCar")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Create([FromBody] CreateCarRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateCarCommand(
                request.Make,
                request.Model,
                request.Year,
                request.DescriptionEn,
                request.DescriptionAr,
                request.DescriptionNl),
            ct);

        return result.Match(
            response => CreatedAtRoute(
                routeName: "GetCarById",
                routeValues: new { version = "1.0", carId = response.Id },
                value: response),
            Problem);
    }

    [HttpPut("{carId:guid}")]
    [ProducesResponseType(typeof(CarDto), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Updates an existing car.")]
    [EndpointDescription("Updates a car's details.")]
    [EndpointName("UpdateCar")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Update(Guid carId, [FromBody] UpdateCarRequest request, CancellationToken ct)
    {
        if (carId != request.Id)
        {
            return this.BadRequest();
        }

        var result = await sender.Send(
            new UpdateCarCommand(
                request.Id,
                request.Make,
                request.Model,
                request.Year,
                request.DescriptionEn,
                request.DescriptionAr,
                request.DescriptionNl),
            ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpDelete("{carId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Removes a car.")]
    [EndpointDescription("Deletes the specified car from the system.")]
    [EndpointName("RemoveCar")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Delete(Guid carId, CancellationToken ct)
    {
        var result = await sender.Send(new RemoveCarCommand(carId), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}
