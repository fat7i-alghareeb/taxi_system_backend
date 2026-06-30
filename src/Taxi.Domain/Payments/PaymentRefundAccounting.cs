namespace Taxi.Domain.Payments;

public static class PaymentRefundAccounting
{
    public static PaymentRefundTotals Calculate(decimal capturedAmount, IEnumerable<PaymentRefund> refunds)
    {
        var normalizedCapturedAmount = Math.Max(0m, capturedAmount);
        var refundList = refunds.ToList();

        var successfulAmount = refundList
            .Where(refund => refund.Status == PaymentRefundStatus.Succeeded)
            .Sum(refund => refund.Amount);

        var reservedAmount = refundList
            .Where(refund => refund.Status is
                PaymentRefundStatus.Requested or
                PaymentRefundStatus.Pending or
                PaymentRefundStatus.Retrying)
            .Sum(refund => refund.Amount);

        var failedAmount = refundList
            .Where(refund => refund.Status is
                PaymentRefundStatus.Failed or
                PaymentRefundStatus.RequiresAdminAction or
                PaymentRefundStatus.PermanentlyFailed)
            .Sum(refund => refund.Amount);

        var availableAmount = Math.Max(0m, normalizedCapturedAmount - successfulAmount - reservedAmount);

        return new PaymentRefundTotals(
            Math.Round(successfulAmount, 2, MidpointRounding.AwayFromZero),
            Math.Round(reservedAmount, 2, MidpointRounding.AwayFromZero),
            Math.Round(failedAmount, 2, MidpointRounding.AwayFromZero),
            Math.Round(availableAmount, 2, MidpointRounding.AwayFromZero),
            normalizedCapturedAmount > 0 && successfulAmount >= normalizedCapturedAmount);
    }
}
