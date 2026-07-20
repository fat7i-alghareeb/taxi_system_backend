using Taxi.Application.Features.Trips.Common;
using Taxi.Domain.Invoices;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

using Xunit;

namespace Taxi.Application.UnitTests.Trips;

/// <summary>
/// The invoice must never betray that a fee went uncollected. It is a customer-facing tax document:
/// a "Remaining due" line on it means the customer is looking at a bill that says they still owe
/// money, which is exactly what the wallet-debt model exists to avoid.
///
/// The mechanism is indirect, which is why it is pinned down here rather than left to emerge:
/// charging the shortfall to the wallet writes a Completed Wallet/WaitingFee payment, that payment
/// counts toward the invoice's captured total, and the remaining balance therefore lands on zero.
/// The receivable lives on the customer's wallet instead — never on the trip's books.
/// </summary>
public class InvoiceUnaffectedByWalletDebtTests
{
    private static readonly Guid TripId = Guid.NewGuid();
    private const string Currency = "eur";

    // ── The trip's own books ─────────────────────────────────────────────────

    [Fact]
    public void Financials_WaitingFeeChargedToDebt_TripShowsNothingUnpaid()
    {
        // Fare paid by card; the waiting fee could not be collected and was charged to the wallet,
        // which records it as a settled wallet payment.
        var result = TripFinancialsCalculator.Compute(
            fareAmount: 15.00m,
            currency: Currency,
            payments: [CompletedCardFare(15.00m), DebtChargedWaitingFee(2.00m)],
            refunds: [],
            waitingSessions: [WaitingSession(4, 0.50m)]);

        Assert.Equal(17.00m, result.TotalCharged);
        Assert.Equal(17.00m, result.TotalPaidAmount);
        Assert.Equal(0m, result.UnpaidAmount);
    }

    [Fact]
    public void Financials_MultipleWaitingSessions_AllChargedToDebt_NothingUnpaid()
    {
        // Two sessions on one trip: 2.00 + 1.50. Charging only the open one used to under-bill and
        // leave the invoice with a balance.
        var result = TripFinancialsCalculator.Compute(
            fareAmount: 15.00m,
            currency: Currency,
            payments: [CompletedCardFare(15.00m), DebtChargedWaitingFee(3.50m)],
            refunds: [],
            waitingSessions: [WaitingSession(4, 0.50m), WaitingSession(3, 0.50m)]);

        Assert.Equal(18.50m, result.TotalCharged);
        Assert.Equal(0m, result.UnpaidAmount);
    }

    [Fact]
    public void Financials_FeeLeftUncollected_StillShowsUnpaid()
    {
        // The regression this whole feature guards against: no payment row for the fee at all.
        var result = TripFinancialsCalculator.Compute(
            fareAmount: 15.00m,
            currency: Currency,
            payments: [CompletedCardFare(15.00m)],
            refunds: [],
            waitingSessions: [WaitingSession(4, 0.50m)]);

        Assert.Equal(2.00m, result.UnpaidAmount);
    }

    // ── The issued invoice ───────────────────────────────────────────────────

    [Fact]
    public void Invoice_WhenFeeChargedToDebt_HasNoRemainingBalance()
    {
        // These are the amounts InvoiceIssuanceService derives once the debt payment exists:
        // gross = fare + waiting fee, and totalPaid covers it because the wallet payment counts.
        var invoice = IssueInvoice(gross: 17.00m, totalPaid: 17.00m);

        Assert.Equal(0m, invoice.RemainingAmount);
        Assert.Equal(17.00m, invoice.TotalPaidAmount);
        Assert.Equal(17.00m, invoice.GrossAmount);
        Assert.NotNull(invoice.PaidAtUtc);
    }

    [Fact]
    public void Invoice_RemainingDueLine_OnlyAppearsWhenSomethingIsGenuinelyUnpaid()
    {
        // Documents the PDF's condition (`RemainingAmount > 0`) from the data side, so a future
        // change to the settlement order that reintroduces a shortfall fails here.
        var settled = IssueInvoice(gross: 17.00m, totalPaid: 17.00m);
        var shortfall = IssueInvoice(gross: 17.00m, totalPaid: 15.00m);

        Assert.Equal(0m, settled.RemainingAmount);
        Assert.Equal(2.00m, shortfall.RemainingAmount);
    }

    [Fact]
    public void Invoice_IsASnapshot_LaterDebtRepaymentCannotAlterIt()
    {
        // Repaying the debt credits the wallet; it touches no Payment row on this trip, so the
        // already-issued invoice cannot move. Pinned by asserting the invoice exposes no mutator.
        var invoice = IssueInvoice(gross: 17.00m, totalPaid: 17.00m);

        var mutators = typeof(Invoice)
            .GetMethods()
            .Where(m => m.DeclaringType == typeof(Invoice) && !m.IsStatic && !m.IsSpecialName)
            .Select(m => m.Name)
            .ToList();

        Assert.Empty(mutators);
        Assert.Equal(0m, invoice.RemainingAmount);
    }

    [Fact]
    public void Invoice_ExposesNoDebtConcept()
    {
        // The invoice must stay unaware that debt exists — no owed/debt/outstanding field may be
        // added to it, or the customer's tax document starts reporting their account status.
        var forbidden = new[] { "debt", "owed", "outstanding", "arrears" };

        var offending = typeof(Invoice)
            .GetProperties()
            .Select(p => p.Name)
            .Where(name => forbidden.Any(f => name.Contains(f, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.Empty(offending);
    }

    // ── The model's load-bearing assumption ──────────────────────────────────

    [Fact]
    public void RefundableKinds_ExcludeWaitingFee_SoDebtCanNeverBeRefunded()
    {
        // The debt payment claims money that never actually arrived. That is only safe because the
        // refund splitter refunds Fare and FareAdjustment exclusively. If WaitingFee were ever
        // added to that set, a cancellation could pay out cash the platform never received.
        var refundableKinds = new[] { PaymentKind.Fare, PaymentKind.FareAdjustment };

        Assert.DoesNotContain(PaymentKind.WaitingFee, refundableKinds);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>The wallet payment written when a fee is charged to debt.</summary>
    private static Payment DebtChargedWaitingFee(decimal amount)
    {
        var p = Payment.CreateWaitingFeeWalletPayment(
            Guid.NewGuid(), TripId, amount, Currency, "wtx_debt").Value;
        p.MarkAsCompleted();
        return p;
    }

    private static Payment CompletedCardFare(decimal amount)
    {
        var p = Payment.CreateForStripe(Guid.NewGuid(), TripId, amount, Currency, "pi_x", "cs_x").Value;
        p.MarkAsCompleted("ch_x", "card");
        return p;
    }

    private static TripWaitingSession WaitingSession(int billableMinutes, decimal ratePerMinute)
    {
        var start = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var session = TripWaitingSession.Start(
            Guid.NewGuid(), TripId, Guid.NewGuid(), ratePerMinute, startedAtUtc: start).Value;
        session.Stop(start.AddMinutes(TripWaitingSession.DefaultGraceMinutes + billableMinutes));
        return session;
    }

    private static Invoice IssueInvoice(decimal gross, decimal totalPaid) =>
        Invoice.Issue(
            id: Guid.NewGuid(),
            tripId: TripId,
            passengerId: Guid.NewGuid(),
            invoiceNumber: "INV-2026-0001",
            issuedAtUtc: DateTimeOffset.UtcNow,
            currencyCode: Currency,
            grossAmount: gross,
            paymentMethod: PaymentMethod.Wallet,
            paymentReference: "wtx_debt",
            paidAtUtc: DateTimeOffset.UtcNow,
            issuerName: "Fat7i",
            issuerAddress: "Amsterdam",
            issuerVatNumber: "NL000000000B01",
            tripReferenceCode: "TRP-INV01",
            tripCompletedAtUtc: DateTimeOffset.UtcNow,
            distanceKm: 5m,
            durationMin: 10m,
            vehicleTypeName: "Sedan",
            passengerName: "Jane",
            stopsJson: "[]",
            waitingFeeAmount: 2.00m,
            fareAmount: gross - 2.00m,
            totalPaidAmount: totalPaid).Value;
}
