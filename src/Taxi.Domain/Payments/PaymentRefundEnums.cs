namespace Taxi.Domain.Payments;

public enum PaymentRefundStatus
{
    Requested,
    Pending,
    Succeeded,
    Failed,
    Retrying,
    RequiresAdminAction,
    PermanentlyFailed,
    Cancelled
}

public enum PaymentRefundSourceType
{
    PassengerCancellation,
    AdminCancellation,
    DriverCancellation,
    AirportWaitCancellation,
    CompensationClaim,
    ManualIncidentRefund,
    AdminRetry,
    NoDriverCancellation,

    /// <summary>
    /// A partial refund of the fare after a mid-trip edit lowered the price
    /// (shorter destination / smaller vehicle). Nets against the fare, not a
    /// cancellation refund.
    /// </summary>
    FareAdjustment
}
