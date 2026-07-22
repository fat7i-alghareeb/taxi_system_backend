namespace Taxi.Application.Common.Options;

/// <summary>
/// Controls the background job that reconciles refunds stuck in Pending because their
/// confirming Stripe webhook never arrived. Bound from the <c>RefundReconciliation</c>
/// section in appsettings.
/// </summary>
public sealed class RefundReconciliationOptions
{
    public const string SectionName = "RefundReconciliation";

    /// <summary>How often the job scans for stuck Pending refunds.</summary>
    public int PollIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// A Pending refund is only reconciled once it has been pending at least this long,
    /// so normal in-flight refunds are never mistaken for stuck ones.
    /// </summary>
    public int StuckPendingThresholdMinutes { get; set; } = 15;
}
