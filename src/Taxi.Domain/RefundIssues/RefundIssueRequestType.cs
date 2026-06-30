namespace Taxi.Domain.RefundIssues;

public enum RefundIssueRequestType
{
    DidNotReceiveRefund = 1,
    ReceivedLessThanExpected = 2,
    RefundTakingTooLong = 3,
    QuestionAboutRefund = 4,
    KnownFailedRefundReview = 5,
    Other = 99,
}
