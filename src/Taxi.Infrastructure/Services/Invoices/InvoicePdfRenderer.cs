using System.Globalization;
using System.Reflection;
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

/// <summary>
/// Renders the branded Fat7i invoice PDF (logo, orange/black theme,
/// billed-to + trip cards, black line-item table, payment-method card and a
/// contact footer). VAT (BTW) is intentionally not itemised.
/// </summary>
public sealed class InvoicePdfRenderer(IStringLocalizerFactory localizerFactory) : IInvoicePdfRenderer
{
    private const string Ink = "#15161A";
    private const string Surface = "#F4F5F7";
    private const string CardBorder = "#E6E7EB";
    private const string Brand = "#F4791F";
    private const string TableHeader = "#15161A";
    private const string Muted = "#6B7280";
    private const string Green = "#16A34A";

    private const string LatinFont = "Lato";
    private const string ArabicFont = "Cairo";

    // Fat7i legal/registration details printed under the brand name.
    // Street, postal code and registration numbers are fixed; only the country
    // word and the "KvK"/"BTW ID" labels are localized.
    private const string CompanyStreet = "Frederik Hendrikstraat, 30zw";
    private const string CompanyPostalCity = "3143LD Maassluis";
    private const string CompanyKvkNumber = "90298934";
    private const string CompanyBtwNumber = "NL004808140B65";

    private static readonly byte[]? LogoBytes = LoadLogo();

    public byte[] Render(Invoice invoice, string languageCode, InvoiceContact contact)
    {
        var localizer = localizerFactory.Create("Taxi.Api.SharedResource", "Taxi.Api");
        var lang = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode;
        var culture = new CultureInfo(lang);
        var previousCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = culture;
            return BuildDocument(invoice, contact, localizer, culture)
                .WithSettings(new DocumentSettings
                {
                    // Keep the output small even though the source logo is high-res.
                    ImageRasterDpi = 144,
                    ImageCompressionQuality = ImageCompressionQuality.Medium,
                })
                .GeneratePdf();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    private static Document BuildDocument(
        Invoice invoice,
        InvoiceContact contact,
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
        // even on Arabic invoices, per business requirement. Known symbols are
        // prefixed (€0.53); otherwise the ISO code is appended (0.53 PLN).
        string FormatMoney(decimal amount)
        {
            var number = amount.ToString("N2", CultureInfo.InvariantCulture);
            var (symbol, isPrefix) = CurrencyAffix(invoice.CurrencyCode);
            return isPrefix ? symbol + number : number + " " + symbol;
        }

        string FormatDate(DateTimeOffset value) =>
            value.ToLocalTime().ToString("dd MMM yyyy", culture);

        string FormatDateTime(DateTimeOffset value) =>
            value.ToLocalTime().ToString("dd MMM yyyy, HH:mm", culture);

        var isPaid = invoice.PaidAtUtc is not null;
        var route = ParseRoute(invoice.StopsJson);
        var methodLabel = ResolvePaymentMethod(invoice, T);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(34);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(s => s
                    .FontSize(10)
                    .FontColor(Ink)
                    .FontFamily(primaryFont, LatinFont, ArabicFont));
                if (rtl)
                {
                    page.ContentFromRightToLeft();
                }

                page.Content().Column(root =>
                {
                    root.Spacing(16);

                    // ---- Header: logo + "Factuur" ----------------------------------
                    root.Item().Row(row =>
                    {
                        row.RelativeItem().AlignMiddle().Height(64).AlignLeft().Element(e =>
                        {
                            if (LogoBytes is not null)
                            {
                                e.MaxHeight(64).Image(LogoBytes).FitHeight();
                            }
                            else
                            {
                                e.Text(invoice.IssuerName).FontSize(20).Bold().FontColor(Brand);
                            }
                        });

                        row.RelativeItem().AlignMiddle().AlignRight()
                            .Text(T(LocalizationKeys.Invoice.Title, "Invoice"))
                            .FontSize(34).Bold().FontColor(Ink);
                    });

                    // ---- Issuer details + meta (number/date/status) -----------------
                    root.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text(invoice.IssuerName).FontSize(17).Bold().FontColor(Brand);
                            left.Item().PaddingTop(1).Text(CompanyStreet).FontSize(9.5f).FontColor(Ink);
                            left.Item().PaddingTop(1).Text(CompanyPostalCity).FontSize(9.5f).FontColor(Ink);
                            left.Item().PaddingTop(1)
                                .Text(T(LocalizationKeys.Invoice.CompanyCountry, "Netherlands"))
                                .FontSize(9.5f).FontColor(Ink);

                            left.Item().PaddingTop(6).Text(text =>
                            {
                                text.Span($"{T(LocalizationKeys.Invoice.CompanyKvk, "KvK")}: ")
                                    .SemiBold().FontColor(Ink);
                                text.Span(CompanyKvkNumber).FontColor(Muted);
                            });
                            left.Item().PaddingTop(1).Text(text =>
                            {
                                text.Span($"{T(LocalizationKeys.Invoice.CompanyBtwId, "BTW ID")}: ")
                                    .SemiBold().FontColor(Ink);
                                text.Span(CompanyBtwNumber).FontColor(Muted);
                            });
                        });

                        row.ConstantItem(230).Column(right =>
                        {
                            MetaRow(right, T(LocalizationKeys.Invoice.Number, "Invoice No."), invoice.InvoiceNumber);
                            MetaRow(right, T(LocalizationKeys.Invoice.Date, "Date"), FormatDate(invoice.IssuedAtUtc));

                            right.Item().PaddingTop(6).Row(r =>
                            {
                                r.RelativeItem().AlignMiddle()
                                    .Text(T(LocalizationKeys.Invoice.Status, "Status"))
                                    .FontSize(9.5f).SemiBold().FontColor(Muted);
                                r.ConstantItem(120).AlignLeft().Element(e =>
                                {
                                    if (isPaid)
                                    {
                                        e.Background(Green).PaddingVertical(3).PaddingHorizontal(12)
                                            .Text(T(LocalizationKeys.Invoice.StatusPaid, "Paid"))
                                            .FontColor(Colors.White).SemiBold().FontSize(10);
                                    }
                                    else
                                    {
                                        e.Text("—").FontColor(Muted);
                                    }
                                });
                            });
                        });
                    });

                    root.Item().LineHorizontal(1).LineColor(Brand);

                    // ---- Billed-to + trip cards ------------------------------------
                    root.Item().Row(row =>
                    {
                        row.RelativeItem().Element(e => Card(e, T(LocalizationKeys.Invoice.BilledTo, "Billed to"), col =>
                        {
                            col.Item().Text(invoice.PassengerName ?? "—").FontSize(12).SemiBold();
                            if (!string.IsNullOrWhiteSpace(invoice.PassengerPhone))
                            {
                                col.Item().PaddingTop(3).Text(invoice.PassengerPhone).FontColor(Muted);
                            }

                            if (!string.IsNullOrWhiteSpace(invoice.PassengerEmail))
                            {
                                col.Item().PaddingTop(3).Text(invoice.PassengerEmail).FontColor(Muted);
                            }

                            if (!string.IsNullOrWhiteSpace(invoice.PassengerAddress))
                            {
                                col.Item().PaddingTop(3).Text(invoice.PassengerAddress).FontColor(Muted);
                            }
                        }));

                        row.ConstantItem(14);

                        row.RelativeItem().Element(e => Card(e, T(LocalizationKeys.Invoice.TripDetails, "Trip details"), col =>
                        {
                            col.Item().Text(T(LocalizationKeys.Invoice.ServiceTitle, "Taxi service")).FontSize(12).SemiBold();
                            if (!string.IsNullOrWhiteSpace(route))
                            {
                                col.Item().PaddingTop(3).Text(text =>
                                {
                                    text.Span($"{T(LocalizationKeys.Invoice.Route, "Route")}: ").FontColor(Muted);
                                    text.Span(route!).FontColor(Ink);
                                });
                            }

                            col.Item().PaddingTop(2).Text(text =>
                            {
                                text.Span($"{T(LocalizationKeys.Invoice.RideDate, "Trip date")}: ").FontColor(Muted);
                                text.Span(FormatDate(invoice.TripCompletedAtUtc ?? invoice.IssuedAtUtc)).FontColor(Ink);
                            });

                            col.Item().PaddingTop(2).Text(text =>
                            {
                                text.Span($"{T(LocalizationKeys.Invoice.ServiceType, "Service type")}: ").FontColor(Muted);
                                text.Span(invoice.VehicleTypeName).FontColor(Ink);
                            });
                        }));
                    });

                    // ---- Line-item table (no VAT column) ---------------------------
                    root.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(5f);
                            c.RelativeColumn(1.3f);
                            c.RelativeColumn(2f);
                        });

                        table.Header(header =>
                        {
                            HeaderCell(header.Cell(), T(LocalizationKeys.Invoice.ColumnDescription, "Description"), Alignment.Left);
                            HeaderCell(header.Cell(), T(LocalizationKeys.Invoice.ColumnQuantity, "Qty"), Alignment.Center);
                            HeaderCell(header.Cell(), T(LocalizationKeys.Invoice.ColumnTotal, "Total"), Alignment.Right);
                        });

                        // Transport line item. When a waiting fee accrued it is split
                        // out onto its own line, so this line shows the fare only.
                        var transportAmount = invoice.GrossAmount - invoice.WaitingFeeAmount;
                        table.Cell().PaddingVertical(8).PaddingHorizontal(10).Column(c =>
                        {
                            c.Item().Text(T(LocalizationKeys.Invoice.ServiceTitle, "Taxi service")).SemiBold();
                            if (!string.IsNullOrWhiteSpace(route))
                            {
                                c.Item().Text(route!).FontSize(9).FontColor(Muted);
                            }

                            c.Item().Text($"{T(LocalizationKeys.Invoice.RideDate, "Trip date")}: {FormatDate(invoice.TripCompletedAtUtc ?? invoice.IssuedAtUtc)}")
                                .FontSize(9).FontColor(Muted);
                        });
                        table.Cell().PaddingVertical(8).AlignCenter().AlignMiddle().Text("1");
                        table.Cell().PaddingVertical(8).PaddingHorizontal(10).AlignRight().AlignMiddle()
                            .Text(FormatMoney(transportAmount)).SemiBold();

                        // Waiting-fee line item (only when one accrued).
                        if (invoice.WaitingFeeAmount > 0m)
                        {
                            table.Cell().PaddingVertical(8).PaddingHorizontal(10).Column(c =>
                            {
                                c.Item().Text(T(LocalizationKeys.Invoice.WaitingFee, "Waiting fee")).SemiBold();
                            });
                            table.Cell().PaddingVertical(8).AlignCenter().AlignMiddle().Text("1");
                            table.Cell().PaddingVertical(8).PaddingHorizontal(10).AlignRight().AlignMiddle()
                                .Text(FormatMoney(invoice.WaitingFeeAmount)).SemiBold();
                        }
                    });

                    // ---- Totals card (right) ---------------------------------------
                    root.Item().Row(row =>
                    {
                        row.RelativeItem();
                        row.ConstantItem(250).Border(1).BorderColor(Brand).Padding(12).Column(totals =>
                        {
                            // VAT breakdown (only when a rate applies). Prices are
                            // VAT-inclusive, so net + VAT = gross.
                            if (invoice.TaxRate > 0m)
                            {
                                totals.Item().Row(r =>
                                {
                                    r.RelativeItem().Text(T(LocalizationKeys.Invoice.Subtotal, "Subtotal (excl. VAT)")).FontSize(10).FontColor(Muted);
                                    r.RelativeItem().AlignRight()
                                        .Text(FormatMoney(invoice.NetAmount)).FontSize(10).FontColor(Ink);
                                });

                                var vatPercent = (invoice.TaxRate * 100m).ToString("0.##", CultureInfo.InvariantCulture);
                                totals.Item().PaddingTop(2).Row(r =>
                                {
                                    r.RelativeItem().Text($"{T(LocalizationKeys.Invoice.Vat, "VAT")} ({vatPercent}%)").FontSize(10).FontColor(Muted);
                                    r.RelativeItem().AlignRight()
                                        .Text(FormatMoney(invoice.TaxAmount)).FontSize(10).FontColor(Ink);
                                });

                                totals.Item().PaddingVertical(4).LineHorizontal(0.5f).LineColor(Surface);
                            }

                            totals.Item().Row(r =>
                            {
                                r.RelativeItem().Text(T(LocalizationKeys.Invoice.Total, "Total")).FontSize(15).Bold();
                                r.RelativeItem().AlignRight()
                                    .Text(FormatMoney(invoice.GrossAmount)).FontSize(15).Bold().FontColor(Brand);
                            });
                            totals.Item().PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem().Text(T(LocalizationKeys.Invoice.TotalPaid, "Total paid")).FontSize(9.5f).FontColor(Muted);
                                r.RelativeItem().AlignRight()
                                    .Text(FormatMoney(isPaid ? invoice.GrossAmount : 0m)).FontSize(9.5f).FontColor(Muted);
                            });
                        });
                    });

                    // ---- Payment-method card ---------------------------------------
                    root.Item().Element(e => Card(e, T(LocalizationKeys.Invoice.PaymentMethod, "Payment method"), col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Column(info =>
                            {
                                info.Item().Text(text =>
                                {
                                    text.Span($"{T(LocalizationKeys.Invoice.PaidVia, "Paid via")}: ").FontColor(Muted);
                                    text.Span(methodLabel).SemiBold().FontColor(Ink);
                                });
                            });

                            if (isPaid)
                            {
                                r.ConstantItem(170).AlignRight().Column(paid =>
                                {
                                    paid.Item().AlignRight()
                                        .Text(T(LocalizationKeys.Invoice.PaymentCompleted, "Payment completed"))
                                        .FontColor(Green).SemiBold();
                                    paid.Item().AlignRight()
                                        .Text(FormatDateTime(invoice.PaidAtUtc!.Value)).FontSize(9).FontColor(Muted);
                                });
                            }
                        });
                    }));
                });

                // ---- Footer: contact row --------------------------------------------
                page.Footer().Column(footer =>
                {
                    footer.Item().PaddingTop(8).LineHorizontal(1).LineColor(Surface);
                    footer.Item().PaddingTop(6).AlignCenter().Row(row =>
                    {
                        var parts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(contact.Email))
                        {
                            parts.Add(contact.Email);
                        }

                        if (!string.IsNullOrWhiteSpace(contact.Website))
                        {
                            parts.Add(contact.Website);
                        }

                        if (!string.IsNullOrWhiteSpace(contact.Phone))
                        {
                            parts.Add(contact.Phone);
                        }

                        if (parts.Count == 0)
                        {
                            row.RelativeItem().AlignCenter().Text(invoice.IssuerName).FontSize(8).FontColor(Muted);
                        }
                        else
                        {
                            row.RelativeItem().AlignCenter()
                                .Text(string.Join("       •       ", parts)).FontSize(9).FontColor(Muted);
                        }
                    });
                });
            });
        });
    }

    private static void MetaRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().PaddingTop(3).Row(r =>
        {
            r.RelativeItem().Text(label).FontSize(9.5f).SemiBold().FontColor(Muted);
            r.ConstantItem(120).Text(value).FontSize(10).FontColor(Ink);
        });
    }

    private static void Card(IContainer container, string title, Action<ColumnDescriptor> body)
    {
        container.Background(Surface).Border(1).BorderColor(CardBorder).Padding(12).Column(col =>
        {
            col.Item().PaddingBottom(6)
                .Text(title.ToUpperInvariant()).FontSize(9).Bold().FontColor(Brand).LetterSpacing(0.05f);
            body(col);
        });
    }

    private static void HeaderCell(IContainer cell, string text, Alignment alignment)
    {
        var styled = cell.Background(TableHeader).PaddingVertical(7).PaddingHorizontal(10);
        styled = alignment switch
        {
            Alignment.Right => styled.AlignRight(),
            Alignment.Center => styled.AlignCenter(),
            _ => styled,
        };
        styled.Text(text.ToUpperInvariant()).FontSize(9).SemiBold().FontColor(Brand);
    }

    private enum Alignment
    {
        Left,
        Center,
        Right,
    }

    /// <summary>Builds "origin → destination" from the stored stop snapshot.</summary>
    private static string? ParseRoute(string stopsJson)
    {
        if (string.IsNullOrWhiteSpace(stopsJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(stopsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var labels = doc.RootElement.EnumerateArray()
                .Select(e => e.TryGetProperty("label", out var l) ? l.GetString() : null)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l!.Trim())
                .ToList();

            return labels.Count switch
            {
                0 => null,
                1 => labels[0],
                _ => $"{labels[0]} → {labels[^1]}",
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Prefers the exact Stripe method (ideal/klarna/card); otherwise falls back
    /// to the coarse payment-method enum.
    /// </summary>
    private static string ResolvePaymentMethod(Invoice invoice, Func<string, string, string> t)
    {
        var stripeType = invoice.StripePaymentMethodType?.Trim().ToLowerInvariant();
        switch (stripeType)
        {
            case "ideal":
                return t(LocalizationKeys.Invoice.MethodIdeal, "iDEAL");
            case "klarna":
                return t(LocalizationKeys.Invoice.MethodKlarna, "Klarna");
            case "card":
            case "card_present":
                return t(LocalizationKeys.Invoice.PaymentCard, "Card");
            case not null when stripeType.Length > 0:
                // Unknown but present — show it capitalised (e.g. "Bancontact").
                return char.ToUpperInvariant(stripeType[0]) + stripeType[1..];
        }

        return invoice.PaymentMethod switch
        {
            PaymentMethod.Cash => t(LocalizationKeys.Invoice.PaymentCash, "Cash"),
            PaymentMethod.CreditCard => t(LocalizationKeys.Invoice.PaymentCard, "Card"),
            PaymentMethod.Wallet => t(LocalizationKeys.Invoice.PaymentWallet, "Wallet"),
            _ => t(LocalizationKeys.Invoice.PaymentCard, "Card"),
        };
    }

    private static (string Symbol, bool IsPrefix) CurrencyAffix(string currencyCode) =>
        currencyCode?.ToUpperInvariant() switch
        {
            "EUR" => ("€", true),
            "USD" => ("$", true),
            "GBP" => ("£", true),
            _ => (currencyCode ?? string.Empty, false),
        };

    private static byte[]? LoadLogo()
    {
        var assembly = typeof(InvoicePdfRenderer).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("oranje_logo.png", StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
        {
            return null;
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static bool IsRtl(string cultureName)
    {
        var name = cultureName?.ToLowerInvariant() ?? string.Empty;
        return name.StartsWith("ar") || name.StartsWith("he") || name.StartsWith("fa") || name.StartsWith("ur");
    }
}
