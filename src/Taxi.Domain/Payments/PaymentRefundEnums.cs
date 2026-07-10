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
    NoDriverCancellation
}
