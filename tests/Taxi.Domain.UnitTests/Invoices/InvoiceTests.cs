using Taxi.Domain.Invoices;
using Taxi.Domain.Payments;
using Xunit;

namespace Taxi.Domain.UnitTests.Invoices;

public class InvoiceTests
{
    [Fact]
    public void Issue_OnTimeTrip_RecordsZeroWaitingFeeAndNoRemainingAmount()
    {
        var invoice = Issue(
            grossAmount: 0.10m,
            fareAmount: 0.10m,
            waitingFeeAmount: 0m,
            totalPaidAmount: 0.10m,
            remainingAmount: 0m,
            taxRate: 0.09m);

        Assert.Equal(0.10m, invoice.FareAmount);
        Assert.Equal(0m, invoice.WaitingFeeAmount);
        Assert.Equal(0.10m, invoice.GrossAmount);
        Assert.Equal(0.10m, invoice.TotalPaidAmount);
        Assert.Equal(0m, invoice.RemainingAmount);
        Assert.Equal(invoice.GrossAmount, invoice.NetAmount + invoice.TaxAmount);
    }

    [Fact]
    public void Issue_UnpaidWaitingSurcharge_RecordsWaitingAsRemainingOnly()
    {
        var invoice = Issue(
            grossAmount: 1.40m,
            fareAmount: 0.10m,
            waitingFeeAmount: 1.30m,
            totalPaidAmount: 0.10m,
            remainingAmount: 1.30m);

        Assert.Equal(0.10m, invoice.FareAmount);
        Assert.Equal(1.30m, invoice.WaitingFeeAmount);
        Assert.Equal(1.40m, invoice.GrossAmount);
        Assert.Equal(0.10m, invoice.TotalPaidAmount);
        Assert.Equal(1.30m, invoice.RemainingAmount);
    }

    [Fact]
    public void Issue_PaidWaitingSurcharge_RecordsNoRemainingAmount()
    {
        var invoice = Issue(
            grossAmount: 1.40m,
            fareAmount: 0.10m,
            waitingFeeAmount: 1.30m,
            totalPaidAmount: 1.40m,
            remainingAmount: 0m);

        Assert.Equal(1.30m, invoice.WaitingFeeAmount);
        Assert.Equal(1.40m, invoice.TotalPaidAmount);
        Assert.Equal(0m, invoice.RemainingAmount);
    }

    [Fact]
    public void Issue_RefundedTrip_RecordsRefundWithoutCreatingRemainingDebt()
    {
        var invoice = Issue(
            grossAmount: 10m,
            fareAmount: 10m,
            waitingFeeAmount: 0m,
            totalPaidAmount: 10m,
            refundedAmount: 3m,
            remainingAmount: 0m);

        Assert.Equal(10m, invoice.TotalPaidAmount);
        Assert.Equal(3m, invoice.RefundedAmount);
        Assert.Equal(0m, invoice.RemainingAmount);
    }

    [Fact]
    public void Issue_LegacyCallDefaults_PreserveExistingPaidInvoiceSemantics()
    {
        var paidAtUtc = DateTimeOffset.Parse("2026-07-04T10:00:00Z");

        var invoice = Invoice.Issue(
            id: Guid.NewGuid(),
            tripId: Guid.NewGuid(),
            passengerId: Guid.NewGuid(),
            invoiceNumber: "INV-LEGACY",
            issuedAtUtc: paidAtUtc,
            currencyCode: "EUR",
            grossAmount: 1.40m,
            paymentMethod: PaymentMethod.CreditCard,
            paymentReference: "pi_legacy",
            paidAtUtc: paidAtUtc,
            issuerName: "Fat7i",
            issuerAddress: "Frederik Hendrikstraat, 30zw",
            issuerVatNumber: "NL004808140B65",
            tripReferenceCode: "TRP-OLD",
            tripCompletedAtUtc: paidAtUtc,
            distanceKm: 1m,
            durationMin: 5m,
            vehicleTypeName: "Taxi",
            passengerName: "Passenger",
            stopsJson: "[]",
            waitingFeeAmount: 1.30m).Value;

        Assert.Equal(0.10m, invoice.FareAmount);
        Assert.Equal(1.30m, invoice.WaitingFeeAmount);
        Assert.Equal(1.40m, invoice.TotalPaidAmount);
        Assert.Equal(0m, invoice.RemainingAmount);
    }

    private static Invoice Issue(
        decimal grossAmount,
        decimal fareAmount,
        decimal waitingFeeAmount,
        decimal totalPaidAmount,
        decimal remainingAmount,
        decimal refundedAmount = 0m,
        decimal discountAmount = 0m,
        decimal taxRate = 0m)
    {
        var issuedAtUtc = DateTimeOffset.Parse("2026-07-04T10:00:00Z");

        return Invoice.Issue(
            id: Guid.NewGuid(),
            tripId: Guid.NewGuid(),
            passengerId: Guid.NewGuid(),
            invoiceNumber: "INV-TEST",
            issuedAtUtc: issuedAtUtc,
            currencyCode: "EUR",
            grossAmount: grossAmount,
            paymentMethod: PaymentMethod.CreditCard,
            paymentReference: "pi_test",
            paidAtUtc: totalPaidAmount > 0m ? issuedAtUtc : null,
            issuerName: "Fat7i",
            issuerAddress: "Frederik Hendrikstraat, 30zw",
            issuerVatNumber: "NL004808140B65",
            tripReferenceCode: "TRP-TEST",
            tripCompletedAtUtc: issuedAtUtc,
            distanceKm: 1m,
            durationMin: 5m,
            vehicleTypeName: "Taxi",
            passengerName: "Passenger",
            stopsJson: "[]",
            taxRate: taxRate,
            waitingFeeAmount: waitingFeeAmount,
            fareAmount: fareAmount,
            discountAmount: discountAmount,
            totalPaidAmount: totalPaidAmount,
            refundedAmount: refundedAmount,
            remainingAmount: remainingAmount).Value;
    }
}
