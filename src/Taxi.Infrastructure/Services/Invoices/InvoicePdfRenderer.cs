using System.Globalization;

using Microsoft.Extensions.Localization;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Invoices;

namespace Taxi.Infrastructure.Services.Invoices;

public sealed class InvoicePdfRenderer(IStringLocalizerFactory localizerFactory) : IInvoicePdfRenderer
{
    private const string Ink = "#0B0B0F";
    private const string Surface = "#F2F2F4";
    private const string Brand = "#D79C5C";
    private const string Muted = "#6B7280";

    private const string LatinFont = "Lato";
    private const string ArabicFont = "Noto Sans Arabic";

    public byte[] Render(Invoice invoice, string languageCode)
    {
        var localizer = localizerFactory.Create("Taxi.Api.SharedResource", "Taxi.Api");
        var lang = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode;
        var culture = new CultureInfo(lang);
        var previousCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = culture;
            return BuildDocument(invoice, localizer, culture).GeneratePdf();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    private static Document BuildDocument(
        Invoice invoice,
        IStringLocalizer localizer,
        CultureInfo culture)
    {
        var rtl = IsRtl(culture.Name);
        var primaryFont = rtl ? ArabicFont : LatinFont;

        string T(string key, string fallback)
        {
            var value = localizer[key];
            return value.ResourceNotFound ? fallback : value.Value;
        }

        // Amounts always use invariant culture so digits stay Latin (1,234.50)
        // even on Arabic invoices, per business requirement.
        string FormatMoney(decimal amount) =>
            amount.ToString("N2", CultureInfo.InvariantCulture) + " " + invoice.CurrencyCode;

        string FormatDate(DateTimeOffset value) =>
            value.ToLocalTime().ToString("dd MMM yyyy", culture);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(s => s
                    .FontSize(10)
                    .FontColor(Ink)
                    .FontFamily(primaryFont, LatinFont));
                if (rtl)
                {
                    page.ContentFromRightToLeft();
                }

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text(invoice.IssuerName)
                                .FontSize(20).Bold().FontColor(Brand);
                            left.Item().PaddingTop(2).Text(invoice.IssuerAddress)
                                .FontSize(9).FontColor(Muted);
                            if (!string.IsNullOrWhiteSpace(invoice.IssuerVatNumber))
                            {
                                left.Item().Text($"{T(LocalizationKeys.Invoice.VatNumber, "VAT")}: {invoice.IssuerVatNumber}")
                                    .FontSize(9).FontColor(Muted);
                            }
                        });

                        row.ConstantItem(160).AlignRight().Column(right =>
                        {
                            right.Item().AlignRight()
                                .Text(T(LocalizationKeys.Invoice.Title, "Invoice"))
                                .FontSize(22).Bold().FontColor(Ink);
                            right.Item().PaddingTop(4).AlignRight()
                                .Text($"{T(LocalizationKeys.Invoice.Number, "Invoice No.")}: {invoice.InvoiceNumber}")
                                .FontSize(10);
                            right.Item().AlignRight()
                                .Text($"{T(LocalizationKeys.Invoice.Date, "Date")}: {FormatDate(invoice.IssuedAtUtc)}")
                                .FontSize(10).FontColor(Muted);
                        });
                    });

                    col.Item().PaddingTop(16).LineHorizontal(1).LineColor(Surface);
                });

                page.Content().PaddingVertical(16).Column(col =>
                {
                    // Billed-to + trip refs
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text(T(LocalizationKeys.Invoice.BilledTo, "Billed to"))
                                .FontSize(9).FontColor(Muted);
                            left.Item().PaddingTop(2).Text(invoice.PassengerName ?? "—")
                                .FontSize(12).SemiBold();
                        });

                        row.RelativeItem().AlignRight().Column(right =>
                        {
                            right.Item().AlignRight()
                                .Text($"Trip #{invoice.TripReferenceCode}")
                                .FontSize(11).SemiBold();
                        });
                    });

                    col.Item().PaddingTop(20).Element(content =>
                    {
                        content.Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2.0f);
                                c.RelativeColumn(3.0f);
                                c.RelativeColumn(1.0f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.4f);
                                c.RelativeColumn(1.6f);
                            });

                            table.Header(header =>
                            {
                                static void HeaderCell(IContainer cell, string text, bool alignRight = false)
                                {
                                    var c = cell.PaddingVertical(6)
                                        .BorderBottom(1).BorderColor(Surface);
                                    (alignRight ? c.AlignRight() : c)
                                        .Text(text)
                                        .FontSize(9).SemiBold().FontColor(Ink);
                                }

                                HeaderCell(header.Cell(), T(LocalizationKeys.Invoice.TransactionDate, "Date"));
                                HeaderCell(header.Cell(), T(LocalizationKeys.Invoice.ColumnDescription, "Description"));
                                HeaderCell(header.Cell(), T(LocalizationKeys.Invoice.ColumnQuantity, "Qty"));
                                HeaderCell(header.Cell(), T(LocalizationKeys.Invoice.ColumnTaxRate, "Tax"));
                                HeaderCell(header.Cell(), T(LocalizationKeys.Invoice.ColumnTaxAmount, "Tax amount"), alignRight: true);
                                HeaderCell(header.Cell(), T(LocalizationKeys.Invoice.ColumnNet, "Net amount"), alignRight: true);
                            });

                            static void BodyCell(IContainer cell, string text, bool alignRight = false)
                            {
                                var container = cell.PaddingVertical(6);
                                (alignRight ? container.AlignRight() : container)
                                    .Text(text).FontSize(10);
                            }

                            BodyCell(table.Cell(), FormatDate(invoice.TripCompletedAtUtc ?? invoice.IssuedAtUtc));
                            BodyCell(table.Cell(), T(LocalizationKeys.Invoice.LineItemTransport, "Transport services"));
                            BodyCell(table.Cell(), "1");
                            var taxRateText = invoice.TaxRate > 0
                                ? (invoice.TaxRate * 100).ToString("0.##", CultureInfo.InvariantCulture) + "%"
                                : "—";
                            BodyCell(table.Cell(), taxRateText);
                            BodyCell(table.Cell(), invoice.TaxAmount > 0 ? FormatMoney(invoice.TaxAmount) : "—", alignRight: true);
                            BodyCell(table.Cell(), FormatMoney(invoice.NetAmount), alignRight: true);
                        });
                    });

                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Surface);

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem();
                        row.ConstantItem(220).Column(totals =>
                        {
                            totals.Item().Row(r =>
                            {
                                r.RelativeItem().Text(T(LocalizationKeys.Invoice.TotalNet, "Total"))
                                    .SemiBold();
                                r.ConstantItem(100).AlignRight()
                                    .Text(FormatMoney(invoice.GrossAmount))
                                    .Bold().FontColor(Brand);
                            });
                        });
                    });
                });

                page.Footer().AlignCenter().Text(invoice.IssuerName)
                    .FontSize(8).FontColor(Muted);
            });
        });
    }

    private static bool IsRtl(string cultureName)
    {
        var name = cultureName?.ToLowerInvariant() ?? string.Empty;
        return name.StartsWith("ar") || name.StartsWith("he") || name.StartsWith("fa") || name.StartsWith("ur");
    }
}
