using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.RefundIssues;

public static class RefundIssueErrors
{
    public static readonly Error NotFound = Error.NotFound(
        LocalizationKeys.RefundIssue.NotFound,
        "Refund issue not found.");

    public static readonly Error InvalidRequestType = Error.Validation(
        LocalizationKeys.RefundIssue.InvalidRequestType,
        "Invalid refund issue request type.");

    public static readonly Error InvalidReviewStatus = Error.Validation(
        LocalizationKeys.RefundIssue.InvalidReviewStatus,
        "Invalid refund issue review status.");

    public static readonly Error AlreadyClosed = Error.Conflict(
        LocalizationKeys.RefundIssue.AlreadyClosed,
        "This refund issue is already closed.");
}
