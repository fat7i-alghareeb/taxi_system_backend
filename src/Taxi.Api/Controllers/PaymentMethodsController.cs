using Asp.Versioning;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Taxi.Application.Features.PaymentMethods.Commands.AddPaymentMethod;
using Taxi.Application.Features.PaymentMethods.Commands.CreatePaymentMethodSetupIntent;
using Taxi.Application.Features.PaymentMethods.Commands.DeletePaymentMethod;
using Taxi.Application.Features.PaymentMethods.Commands.SetDefaultPaymentMethod;
using Taxi.Application.Features.PaymentMethods.Dtos;
using Taxi.Application.Features.PaymentMethods.Queries.GetPaymentMethods;
using Taxi.Contracts.Requests.PaymentMethods;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payment-methods")]
public class PaymentMethodsController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Roles = "Passenger")]
    [ProducesResponseType(typeof(List<PaymentMethodDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Returns the current passenger's saved reusable payment methods.")]
    [EndpointName("GetPaymentMethods")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetPaymentMethods(CancellationToken ct)
    {
        var result = await sender.Send(new GetPaymentMethodsQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("setup-intents")]
    [Authorize(Roles = "Passenger")]
    [ProducesResponseType(typeof(PaymentMethodSetupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Starts a Stripe SetupIntent so the passenger can save a reusable payment method.")]
    [EndpointDescription("Returns Stripe payment-sheet (setup mode) details. The card is saved with the customer for future off-session ride-related charges; consent is captured in the app.")]
    [EndpointName("CreatePaymentMethodSetupIntent")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> CreateSetupIntent(CancellationToken ct)
    {
        var result = await sender.Send(new CreatePaymentMethodSetupIntentCommand(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost]
    [Authorize(Roles = "Passenger")]
    [ProducesResponseType(typeof(PaymentMethodDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Persists a reusable payment method after its SetupIntent is confirmed in the app.")]
    [EndpointName("AddPaymentMethod")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> AddPaymentMethod([FromBody] AddPaymentMethodRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new AddPaymentMethodCommand(request.PaymentMethodId, request.SetAsDefault), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPut("{id:guid}/default")]
    [Authorize(Roles = "Passenger")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Sets a saved method as the default reusable method used for automatic ride-related charges.")]
    [EndpointName("SetDefaultPaymentMethod")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new SetDefaultPaymentMethodCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Passenger")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Removes a saved payment method (detaches it from Stripe and soft-deletes it).")]
    [EndpointName("DeletePaymentMethod")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new DeletePaymentMethodCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
