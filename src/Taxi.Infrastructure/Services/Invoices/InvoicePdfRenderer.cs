using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Localization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Invoices;
using Taxi.Domain.Payments;

namespace Taxi.Infrastructure.Services.Invoices;

public sealed class InvoicePdfRenderer(IStringLocalizerFactory localizerFactory) : IInvoicePdfRenderer
{
    private const string Ink = "#0B0B0F";
    private const string Surface = "#F2F2F4";
    private const string Brand = "#FF7A00";
    private const string Muted = "#6B7280";

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
        var stops = ParseStops(invoice.StopsJson);
        var rtl = IsRtl(culture.Name);

        string T(string key, string fallback)
        {
            var value = localizer[key];
            return value.ResourceNotFound ? fallback : value.Value;
        }

        string FormatMoney(decimal amount) =>
            amount.ToString("N2", culture) + " " + invoice.CurrencyCode;

        string FormatDate(DateTimeOffset value) =>
            value.ToLocalTime().ToString("dd MMM yyyy", culture);

        string PaymentLabel(PaymentMethod method) => method switch
        {
            PaymentMethod.Cash => T(LocalizationKeys.Invoice.PaymentCash, "Cash"),
            PaymentMethod.CreditCard => T(LocalizationKeys.Invoice.PaymentCard, "Card"),
            PaymentMethod.Wallet => T(LocalizationKeys.Invoice.PaymentWallet, "Wallet"),
            _ => method.ToString(),
        };

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(s => s.FontSize(10).FontColor(Ink));
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
                            if (invoice.TripCompletedAtUtc is { } completed)
                            {
                                right.Item().AlignRight()
                                    .Text(FormatDate(completed))
                                    .FontSize(9).FontColor(Muted);
                            }
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
                                static void HeaderCell(IContainer cell, string text)
                                {
                                    cell.PaddingVertical(6)
                                        .BorderBottom(1).BorderColor(Surface)
                                        .Text(text)
                                        .FontSize(9).SemiBold().FontColor(Ink);
                                }

                                HeaderCell(header.Cell(), T(LocalizationKeys.Invoice.TransactionDate, "Date"));
                                HeaderCell(header.Cell(), T(LocalizationKeys.Invoice.ColumnDescription, "Description"));
                                HeaderCell(header.Cell(), T(LocalizationKeys.Invoice.ColumnQuantity, "Qty"));
                                HeaderCell(header.Cell(), T(LocalizationKeys.Invoice.ColumnTaxRate, "Tax"));
                                HeaderCell(header.Cell(), T(LocalizationKeys.Invoice.ColumnTaxAmount, "Tax amount"));
                                HeaderCell(header.Cell(), T(LocalizationKeys.Invoice.ColumnNet, "Net amount"));
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
                            BodyCell(table.Cell(), invoice.TaxRate > 0
                                ? (invoice.TaxRate * 100).ToString("0.##", culture) + "%"
                                : "—");
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

                    if (stops.Count > 0)
                    {
                        col.Item().PaddingTop(24).Text("Trip stops").FontSize(11).SemiBold();
                        foreach (var stop in stops)
                        {
                            col.Item().PaddingTop(4).Row(row =>
                            {
                                row.ConstantItem(40).Text(stop.CompletedAtUtc is { } t
                                    ? t.ToLocalTime().ToString("HH:mm", culture)
                                    : "—").FontSize(9).FontColor(Muted);
                                row.RelativeItem().Text(stop.Label ?? "—").FontSize(10);
                            });
                        }
                    }

                    col.Item().PaddingTop(28).Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text(T(LocalizationKeys.Invoice.PaymentMethod, "Payment method"))
                                .FontSize(9).FontColor(Muted);
                            left.Item().Text(PaymentLabel(invoice.PaymentMethod)).FontSize(11).SemiBold();
                            if (!string.IsNullOrWhiteSpace(invoice.PaymentReference))
                            {
                                left.Item().Text(invoice.PaymentReference).FontSize(9).FontColor(Muted);
                            }
                        });

                        row.RelativeItem().AlignRight().Column(right =>
                        {
                            if (invoice.PaidAtUtc is { } paid)
                            {
                                right.Item().AlignRight().Text(FormatDate(paid))
                                    .FontSize(9).FontColor(Muted);
                            }
                        });
                    });
                });

                page.Footer().AlignCenter().Text(invoice.IssuerName)
                    .FontSize(8).FontColor(Muted);
            });
        });
    }

    private static List<InvoiceStopSnapshot> ParseStops(string stopsJson)
    {
        if (string.IsNullOrWhiteSpace(stopsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<InvoiceStopSnapshot>>(stopsJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool IsRtl(string cultureName)
    {
        var name = cultureName?.ToLowerInvariant() ?? string.Empty;
        return name.StartsWith("ar") || name.StartsWith("he") || name.StartsWith("fa") || name.StartsWith("ur");
    }

    private sealed record InvoiceStopSnapshot(int Sequence, string? Label, DateTimeOffset? CompletedAtUtc);
}
