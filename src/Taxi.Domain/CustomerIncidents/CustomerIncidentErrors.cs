using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.CustomerIncidents;

public static class CustomerIncidentErrors
{
    public static Error NotFound =>
        Error.NotFound(LocalizationKeys.CustomerIncident.NotFound, "Customer incident not found.");

    public static Error AlreadyClosed =>
        Error.Conflict(LocalizationKeys.CustomerIncident.AlreadyClosed, "This incident has already been closed.");

    public static Error InvalidStatus =>
        Error.Validation(LocalizationKeys.CustomerIncident.InvalidStatus, "Invalid incident status.");

    public static Error NoRefundablePayment =>
        Error.Validation(LocalizationKeys.CustomerIncident.NoRefundablePayment, "There is no completed payment to refund for this incident.");

    public static Error RefundFailed =>
        Error.Failure(LocalizationKeys.CustomerIncident.RefundFailed, "The refund could not be processed.");
}
