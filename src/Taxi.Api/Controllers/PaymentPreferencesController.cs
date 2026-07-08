using Asp.Versioning;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Taxi.Application.Features.PaymentPreferences.Commands.SetPaymentPreference;
using Taxi.Application.Features.PaymentPreferences.Dtos;
using Taxi.Application.Features.PaymentPreferences.Queries.GetPaymentPreference;
using Taxi.Contracts.Requests.PaymentMethods;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payment-preferences")]
public class PaymentPreferencesController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Roles = "Passenger")]
    [ProducesResponseType(typeof(PaymentPreferenceDto), StatusCodes.Status200OK)]
    [EndpointSummary("Returns the passenger's preferred trip-booking method and the enabled method types.")]
    [EndpointName("GetPaymentPreference")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetPreference(CancellationToken ct)
    {
        var result = await sender.Send(new GetPaymentPreferenceQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPut]
    [Authorize(Roles = "Passenger")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Sets (or clears) the preferred trip-booking method type. Booking only — not used for automatic fees.")]
    [EndpointName("SetPaymentPreference")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> SetPreference([FromBody] SetPaymentPreferenceRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SetPaymentPreferenceCommand(request.PreferredMethodType), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
