using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Queries.GetTripInvoicePdf;

public class GetTripInvoicePdfQueryHandler(
    IAppDbContext context,
    IUser currentUser,
    IInvoicePdfRenderer renderer,
    IInvoiceIssuanceService invoiceIssuance)
    : IRequestHandler<GetTripInvoicePdfQuery, Result<TripInvoicePdfResult>>
{
    /// <summary>
    /// Languages the renderer + SharedResource files cover. Anything else
    /// silently falls back to <see cref="DefaultLanguage"/>.
    /// </summary>
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "nl", "en", "ar", "de", "es", "fr", "pl", "ro", "uk",
    };

    /// <summary>Dutch is the default per business rule (Fat7i is NL-based).</summary>
    private const string DefaultLanguage = "nl";

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

        var bytes = renderer.Render(invoice, languageCode);
        var fileName = $"{invoice.InvoiceNumber}.pdf";

        return new TripInvoicePdfResult(fileName, bytes);
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
