using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.Config.GetAppVersionConfig;
using Taxi.Application.Features.Config.GetBootstrapConfig;
using Taxi.Application.Features.Config.GetClientConfig;
using Taxi.Application.Features.Config.GetCompanyContact;
using Taxi.Application.Features.Config.GetCurrency;
using Taxi.Application.Features.Config.GetSupportContact;
using Taxi.Application.Features.Config.GetTripDiscount;
using Taxi.Application.Features.Config.GetVatRate;
using Taxi.Application.Features.Config.UpdateAppVersionConfig;
using Taxi.Application.Features.Config.UpdateCompanyContact;
using Taxi.Application.Features.Config.UpdateCurrency;
using Taxi.Application.Features.Config.UpdateSupportContact;
using Taxi.Application.Features.Config.UpdateTripDiscount;
using Taxi.Application.Features.Config.UpdateVatRate;
using Taxi.Contracts.Requests.Config;
using Taxi.Contracts.Responses.Config;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/app-config")]
public class AppConfigController(ISender sender) : ApiController
{
    [HttpGet("client")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ClientConfigDto), StatusCodes.Status200OK)]
    [EndpointSummary("Returns public client configuration (Stripe enablement, publishable key).")]
    [EndpointDescription("Consumed by the mobile app at bootstrap to decide whether to initialize Stripe and which publishable key to use.")]
    [EndpointName("GetClientConfig")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetClientConfig(CancellationToken ct)
    {
        var result = await sender.Send(new GetClientConfigQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("trip-discount")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TripDiscountDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets the global trip discount percentage.")]
    [EndpointName("GetTripDiscount")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetTripDiscount(CancellationToken ct)
    {
        var result = await sender.Send(new GetTripDiscountQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPut("trip-discount")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TripDiscountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates the global trip discount percentage (0–100).")]
    [EndpointName("UpdateTripDiscount")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UpdateTripDiscount(
        [FromBody] UpdateTripDiscountRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new UpdateTripDiscountCommand(request.DiscountPercent), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("currency")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets the global currency (ISO 4217 code) used for Stripe payments.")]
    [EndpointName("GetCurrency")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetCurrency(CancellationToken ct)
    {
        var result = await sender.Send(new GetCurrencyQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPut("currency")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates the global currency (ISO 4217 3-letter code) used for Stripe payments.")]
    [EndpointName("UpdateCurrency")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UpdateCurrency(
        [FromBody] UpdateCurrencyRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new UpdateCurrencyCommand(request.CurrencyCode), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("vat-rate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VatRateDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets the VAT/BTW rate (as a fraction, e.g. 0.09 = 9%) applied to invoices.")]
    [EndpointName("GetVatRate")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetVatRate(CancellationToken ct)
    {
        var result = await sender.Send(new GetVatRateQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPut("vat-rate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VatRateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates the VAT/BTW rate as a fraction (0 ≤ rate < 1, e.g. 0.09 = 9%). Prices stay VAT-inclusive; the rate is used to break invoices into net + tax.")]
    [EndpointName("UpdateVatRate")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UpdateVatRate(
        [FromBody] UpdateVatRateRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new UpdateVatRateCommand(request.Rate), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("company-contact")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CompanyContactDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets the company contact details (email, phone, website) printed on invoices.")]
    [EndpointName("GetCompanyContact")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetCompanyContact(CancellationToken ct)
    {
        var result = await sender.Send(new GetCompanyContactQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPut("company-contact")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CompanyContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates the company contact details (email, phone, website) printed on invoices.")]
    [EndpointName("UpdateCompanyContact")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UpdateCompanyContact(
        [FromBody] UpdateCompanyContactRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateCompanyContactCommand(request.Email, request.Phone, request.Website), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("support-contact")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SupportContactDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets the support WhatsApp number used by the in-trip \"Report problem\" action.")]
    [EndpointName("GetSupportContact")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetSupportContact(CancellationToken ct)
    {
        var result = await sender.Send(new GetSupportContactQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPut("support-contact")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SupportContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates the support WhatsApp number used by the in-trip \"Report problem\" action.")]
    [EndpointName("UpdateSupportContact")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UpdateSupportContact(
        [FromBody] UpdateSupportContactRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new UpdateSupportContactCommand(request.WhatsApp), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("app-version")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AppVersionConfigDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets the customer app version gate configuration (master switch, per-platform latest/minimum versions and store URLs).")]
    [EndpointDescription("Consumed by the customer app at bootstrap to decide whether to force an update, suggest one, or do nothing. Anonymous because the check runs before login.")]
    [EndpointName("GetAppVersionConfig")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetAppVersionConfig(CancellationToken ct)
    {
        var result = await sender.Send(new GetAppVersionConfigQuery(), ct);
        return result.Match(Ok, Problem);
    }

    // TODO: replace the "Admin" literal with a shared Roles constant once one exists.
    // Every controller in the solution uses raw role strings today, so introducing the
    // class here would leave the codebase half-converted; tracked as separate cleanup.
    [HttpPut("app-version")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(AppVersionConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates the customer app version gate configuration.")]
    [EndpointDescription("A blank version or store URL clears that value and leaves the platform ungated. The minimum required version may not exceed the latest version.")]
    [EndpointName("UpdateAppVersionConfig")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UpdateAppVersionConfig(
        [FromBody] UpdateAppVersionConfigRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateAppVersionConfigCommand(
                request.Enabled,
                request.Android?.LatestVersion,
                request.Android?.MinimumRequiredVersion,
                request.Android?.StoreUrl,
                request.Ios?.LatestVersion,
                request.Ios?.MinimumRequiredVersion,
                request.Ios?.StoreUrl),
            ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("bootstrap")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BootstrapConfigDto), StatusCodes.Status200OK)]
    [EndpointSummary("Returns the customer app's entire startup payload in one request.")]
    [EndpointDescription("Combines client config, support contact and the version gate so the app makes one round trip at launch instead of three. The individual endpoints remain available for the admin app.")]
    [EndpointName("GetBootstrapConfig")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetBootstrapConfig(CancellationToken ct)
    {
        var result = await sender.Send(new GetBootstrapConfigQuery(), ct);
        return result.Match(Ok, Problem);
    }
}
