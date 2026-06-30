namespace Taxi.Contracts.Requests.RefundIssues;

public record CreateRefundIssueRequest(
    string RequestType,
    string CustomerReason,
    string? Note,
    bool WhatsAppOpened = false);
