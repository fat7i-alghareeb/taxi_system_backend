using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Configuration;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Queries.GetTripInvoicePdf;

public class GetTripInvoicePdfQueryHandler(
    IAppDbContext context,
    IUser currentUser,
    IInvoicePdfRenderer renderer,
    IInvoiceIssuanceService invoiceIssuance)
    : IRequestHandler<GetTripInvoicePdfQuery, Result<TripInvoicePdfResult>>
{
    /// <summary>Dutch is the default per business rule (Fat7i is NL-based).</summary>
    private const string DefaultLanguage = "nl";

    /// <summary>
    /// Languages the renderer + SharedResource files cover. Anything else
    /// silently falls back to <see cref="DefaultLanguage"/>.
    /// </summary>
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "nl", "en", "ar", "de", "es", "fr", "pl", "ro", "uk",
    };

    public async Task<Result<TripInvoicePdfResult>> Handle(GetTripInvoicePdfQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var trip = await context.Trips
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TripId, ct);

        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        if (!currentUser.IsAdmin && trip.PassengerId != userId)
        {
            return TripErrors.NotOwnedByPassenger;
        }

        // Lazy-issue: trips that completed before invoicing was live (or
        // whose eager issuance failed) get their invoice on first PDF request.
        var issued = await invoiceIssuance.EnsureIssuedAsync(trip.Id, ct);
        if (issued.IsFailure)
        {
            return issued.Error;
        }

        var invoice = issued.Value;
        var languageCode = ResolveLanguage(request.LanguageCode);
        var contact = await GetCompanyContactAsync(ct);

        var bytes = renderer.Render(invoice, languageCode, contact);
        var fileName = $"{invoice.InvoiceNumber}.pdf";

        return new TripInvoicePdfResult(fileName, bytes);
    }

    /// <summary>
    /// Reads the live, admin-editable company contact details for the footer.
    /// Rendered live (not snapshotted) so admin edits apply to every download.
    /// </summary>
    private async Task<InvoiceContact> GetCompanyContactAsync(CancellationToken ct)
    {
        var keys = new[] { AppConfigKeys.CompanyEmail, AppConfigKeys.CompanyPhone, AppConfigKeys.CompanyWebsite };

        var values = await context.AppConfigs
            .AsNoTracking()
            .Where(c => keys.Contains(c.Key))
            .ToDictionaryAsync(c => c.Key, c => c.Value, ct);

        return new InvoiceContact(
            Email: values.GetValueOrDefault(AppConfigKeys.CompanyEmail) ?? string.Empty,
            Phone: values.GetValueOrDefault(AppConfigKeys.CompanyPhone) ?? string.Empty,
            Website: values.GetValueOrDefault(AppConfigKeys.CompanyWebsite) ?? string.Empty);
    }

    private static string ResolveLanguage(string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
        {
            return DefaultLanguage;
        }

        var normalized = requested.Trim().ToLowerInvariant();
        return SupportedLanguages.Contains(normalized) ? normalized : DefaultLanguage;
    }
}
