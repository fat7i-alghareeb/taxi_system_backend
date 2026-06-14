using System.Text;

using Microsoft.Extensions.Localization;

using QuestPDF.Infrastructure;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Invoices;
using Taxi.Domain.Payments;
using Taxi.Infrastructure.Services.Invoices;

using Xunit;

namespace Taxi.Api.IntegrationTests;

// TEMP smoke test: verifies the QuestPDF invoice layout renders without throwing.
public class InvoicePdfRenderSmokeTests
{
    private sealed class KeyLocalizer : IStringLocalizer
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] => new(name, name, resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }

    private sealed class StubFactory : IStringLocalizerFactory
    {
        public IStringLocalizer Create(Type resourceSource) => new KeyLocalizer();

        public IStringLocalizer Create(string baseName, string location) => new KeyLocalizer();
    }

    [Fact]
    public void Renders_valid_pdf_bytes()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var invoice = Invoice.Issue(
            id: Guid.NewGuid(),
            tripId: Guid.NewGuid(),
            passengerId: Guid.NewGuid(),
            invoiceNumber: "1",
            issuedAtUtc: DateTimeOffset.UtcNow,
            currencyCode: "EUR",
            grossAmount: 0.53m,
            paymentMethod: PaymentMethod.CreditCard,
            paymentReference: "TRP-0E01CD",
            paidAtUtc: DateTimeOffset.UtcNow,
            issuerName: "Fat7i",
            issuerAddress: "Frederik Hendrikstraat, 30zw\n3143LD Maassluis\nNetherlands",
            issuerVatNumber: "NL004808140B65",
            tripReferenceCode: "ABC123",
            tripCompletedAtUtc: DateTimeOffset.UtcNow,
            distanceKm: 40m,
            durationMin: 35m,
            vehicleTypeName: "Taxi",
            passengerName: "Ahmed Al Ali",
            stopsJson: "[{\"sequence\":0,\"label\":\"Amsterdam\"},{\"sequence\":1,\"label\":\"Utrecht\"}]",
            passengerPhone: "+31 6 12345678",
            stripePaymentMethodType: "ideal").Value;

        var renderer = new InvoicePdfRenderer(new StubFactory());

        foreach (var lang in new[] { "nl", "en", "ar" })
        {
            var bytes = renderer.Render(invoice, lang, new InvoiceContact("info@fat7i.dev", "0639550352", "www.fat7i.dev"));
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 1000, $"PDF too small for {lang}: {bytes.Length} bytes");
            Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        }
    }
}
