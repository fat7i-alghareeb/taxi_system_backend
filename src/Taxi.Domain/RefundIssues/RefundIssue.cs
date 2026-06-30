using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;

namespace Taxi.Domain.RefundIssues;

public sealed class RefundIssue : AuditableEntity
{
    private RefundIssue() { }

    private RefundIssue(
        Guid id,
        Guid passengerId,
        Guid tripId,
        Guid paymentId,
        RefundIssueRequestType requestType,
        string customerReason,
        string? note,
        Guid? paymentRefundId,
        Guid? tripCancellationId,
        PaymentRefundStatus? refundStatusSnapshot,
        decimal? refundAmountSnapshot,
        string? refundCurrencySnapshot,
        bool whatsAppOpened)
        : base(id)
    {
        PassengerId = passengerId;
        TripId = tripId;
        PaymentId = paymentId;
        PaymentRefundId = paymentRefundId;
        TripCancellationId = tripCancellationId;
        RequestType = requestType;
        CustomerReason = customerReason.Trim();
        Note = NormalizeOptional(note);
        RefundStatusSnapshot = refundStatusSnapshot;
        RefundAmountSnapshot = refundAmountSnapshot;
        RefundCurrencySnapshot = NormalizeOptional(refundCurrencySnapshot);
        ReviewStatus = RefundIssueReviewStatus.Open;
        WhatsAppOpened = whatsAppOpened;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid PassengerId { get; private set; }
    public Guid TripId { get; private set; }
    public Guid PaymentId { get; private set; }
    public Guid? PaymentRefundId { get; private set; }
    public Guid? TripCancellationId { get; private set; }
    public RefundIssueRequestType RequestType { get; private set; }
    public string CustomerReason { get; private set; } = string.Empty;
    public string? Note { get; private set; }
    public PaymentRefundStatus? RefundStatusSnapshot { get; private set; }
    public decimal? RefundAmountSnapshot { get; private set; }
    public string? RefundCurrencySnapshot { get; private set; }
    public RefundIssueReviewStatus ReviewStatus { get; private set; }
    public Guid? ReviewedByAdminId { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public string? AdminNotes { get; private set; }
    public bool WhatsAppOpened { get; private set; }

    public static Result<RefundIssue> Create(
        Guid id,
        Guid passengerId,
        Guid tripId,
        Guid paymentId,
        RefundIssueRequestType requestType,
        string customerReason,
        string? note = null,
        Guid? paymentRefundId = null,
        Guid? tripCancellationId = null,
        PaymentRefundStatus? refundStatusSnapshot = null,
        decimal? refundAmountSnapshot = null,
        string? refundCurrencySnapshot = null,
        bool whatsAppOpened = false)
    {
        if (id == Guid.Empty ||
            passengerId == Guid.Empty ||
            tripId == Guid.Empty ||
            paymentId == Guid.Empty ||
            string.IsNullOrWhiteSpace(customerReason) ||
            !Enum.IsDefined(requestType))
        {
            return RefundIssueErrors.InvalidRequestType;
        }

        return new RefundIssue(
            id,
            passengerId,
            tripId,
            paymentId,
            requestType,
            customerReason,
            note,
            NormalizeGuid(paymentRefundId),
            NormalizeGuid(tripCancellationId),
            refundStatusSnapshot,
            refundAmountSnapshot.HasValue ? Math.Round(refundAmountSnapshot.Value, 2, MidpointRounding.AwayFromZero) : null,
            refundCurrencySnapshot,
            whatsAppOpened);
    }

    public Result<Success> Review(RefundIssueReviewStatus status, Guid adminId, string? adminNotes)
    {
        if (!Enum.IsDefined(status) || status == RefundIssueReviewStatus.Open)
        {
            return RefundIssueErrors.InvalidReviewStatus;
        }

        if (ReviewStatus is RefundIssueReviewStatus.Resolved or RefundIssueReviewStatus.Dismissed)
        {
            return RefundIssueErrors.AlreadyClosed;
        }

        ReviewStatus = status;
        ReviewedByAdminId = adminId;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        AppendAdminNote(adminId, adminNotes);
        return Result.Success;
    }

    public Result<Success> MarkWhatsAppOpened()
    {
        WhatsAppOpened = true;
        return Result.Success;
    }

    private void AppendAdminNote(Guid adminId, string? note)
    {
        var normalized = NormalizeOptional(note);
        if (normalized is null)
        {
            return;
        }

        var stamped = $"[{DateTimeOffset.UtcNow:u}] {adminId}: {normalized}";
        AdminNotes = string.IsNullOrWhiteSpace(AdminNotes) ? stamped : $"{AdminNotes}\n{stamped}";
    }

    private static Guid? NormalizeGuid(Guid? id)
        => !id.HasValue || id.Value == Guid.Empty ? null : id;

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
