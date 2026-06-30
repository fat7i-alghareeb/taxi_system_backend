namespace Taxi.Contracts.Requests.RefundIssues;

public record ReviewRefundIssueRequest(string ReviewStatus, string? AdminNotes);
