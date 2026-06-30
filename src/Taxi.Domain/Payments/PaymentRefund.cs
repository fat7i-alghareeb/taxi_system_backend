using Taxi.Contracts.Common;
using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Payments;

public sealed class PaymentRefund : AuditableEntity
{
    private const string DefaultCustomerFailureMessage =
        LocalizationKeys.Payment.RefundCustomerFailureMessage;

    private PaymentRefund() { }

    private PaymentRefund(
        Guid id,
        Guid paymentId,
        PaymentRefundSourceType sourceType,
        decimal amount,
        string currency,
        decimal originalPaymentAmountSnapshot,
        decimal? refundPercent,
        bool isFullRefund,
        Guid? tripId,
        Guid? tripCancellationId,
        Guid? customerIncidentId,
        Guid? tripCompensationClaimId,
        Guid? requestedByAdminId,
        Guid? passengerId,
        string? stripePaymentIntentId,
        string? stripeChargeId,
        string? idempotencyKey)
        : base(id)
    {
        PaymentId = paymentId;
        TripId = tripId;
        TripCancellationId = tripCancellationId;
        CustomerIncidentId = customerIncidentId;
        TripCompensationClaimId = tripCompensationClaimId;
        RequestedByAdminId = requestedByAdminId;
        PassengerId = passengerId;
        SourceType = sourceType;
        Status = PaymentRefundStatus.Requested;
        Amount = amount;
        Currency = currency;
        RefundPercent = refundPercent;
        OriginalPaymentAmountSnapshot = originalPaymentAmountSnapshot;
        IsFullRefund = isFullRefund || amount >= originalPaymentAmountSnapshot;
        StripePaymentIntentId = NormalizeOptional(stripePaymentIntentId);
        StripeChargeId = NormalizeOptional(stripeChargeId);
        IdempotencyKey = NormalizeOptional(idempotencyKey);
        RequestedAtUtc = DateTimeOffset.UtcNow;
        CreatedAtUtc = RequestedAtUtc;
    }

    public Guid PaymentId { get; private set; }
    public Guid? TripId { get; private set; }
    public Guid? TripCancellationId { get; private set; }
    public Guid? CustomerIncidentId { get; private set; }
    public Guid? TripCompensationClaimId { get; private set; }
    public Guid? RequestedByAdminId { get; private set; }
    public Guid? PassengerId { get; private set; }
    public PaymentRefundSourceType SourceType { get; private set; }
    public PaymentRefundStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = default!;
    public decimal? RefundPercent { get; private set; }
    public bool IsFullRefund { get; private set; }
    public decimal OriginalPaymentAmountSnapshot { get; private set; }
    public string? StripeRefundId { get; private set; }
    public string? StripePaymentIntentId { get; private set; }
    public string? StripeChargeId { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public int AttemptCount { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureReason { get; private set; }
    public string? SafeCustomerFailureMessage { get; private set; }
    public bool RequiresAdminAction { get; private set; }
    public bool CanRetry { get; private set; }
    public string? RetryBlockedReason { get; private set; }
    public DateTimeOffset RequestedAtUtc { get; private set; }
    public DateTimeOffset? LastAttemptAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? FailedAtUtc { get; private set; }
    public string? LastStripeEventId { get; private set; }
    public string? AdminNote { get; private set; }

    public static Result<PaymentRefund> Create(
        Guid id,
        Guid paymentId,
        PaymentRefundSourceType sourceType,
        decimal amount,
        string currency,
        decimal originalPaymentAmountSnapshot,
        decimal? refundPercent = null,
        bool isFullRefund = false,
        Guid? tripId = null,
        Guid? tripCancellationId = null,
        Guid? customerIncidentId = null,
        Guid? tripCompensationClaimId = null,
        Guid? requestedByAdminId = null,
        Guid? passengerId = null,
        string? stripePaymentIntentId = null,
        string? stripeChargeId = null,
        string? idempotencyKey = null)
    {
        if (paymentId == Guid.Empty ||
            amount <= 0 ||
            originalPaymentAmountSnapshot <= 0 ||
            amount > originalPaymentAmountSnapshot ||
            string.IsNullOrWhiteSpace(currency) ||
            currency.Trim().Length != 3 ||
            refundPercent is < 0 or > 100)
        {
            return PaymentErrors.InvalidAmount;
        }

        var roundedRefundPercent = refundPercent.HasValue
            ? Math.Round(refundPercent.Value, 2, MidpointRounding.AwayFromZero)
            : (decimal?)null;

        return new PaymentRefund(
            id,
            paymentId,
            sourceType,
            Math.Round(amount, 2, MidpointRounding.AwayFromZero),
            currency.Trim(),
            Math.Round(originalPaymentAmountSnapshot, 2, MidpointRounding.AwayFromZero),
            roundedRefundPercent,
            isFullRefund,
            NormalizeGuid(tripId),
            NormalizeGuid(tripCancellationId),
            NormalizeGuid(customerIncidentId),
            NormalizeGuid(tripCompensationClaimId),
            NormalizeGuid(requestedByAdminId),
            NormalizeGuid(passengerId),
            stripePaymentIntentId,
            stripeChargeId,
            idempotencyKey);
    }

    public Result<Success> MarkAttemptStarted(string idempotencyKey, DateTimeOffset? attemptedAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return PaymentErrors.InvalidAmount;
        }

        AttemptCount++;
        IdempotencyKey = idempotencyKey.Trim();
        LastAttemptAtUtc = attemptedAtUtc ?? DateTimeOffset.UtcNow;
        Status = AttemptCount > 1 ? PaymentRefundStatus.Retrying : PaymentRefundStatus.Requested;
        CanRetry = false;
        RetryBlockedReason = null;
        return Result.Success;
    }

    public Result<Success> MarkPending(
        string stripeRefundId,
        string? stripePaymentIntentId = null,
        string? stripeChargeId = null,
        string? lastStripeEventId = null)
    {
        if (string.IsNullOrWhiteSpace(stripeRefundId))
        {
            return PaymentErrors.InvalidAmount;
        }

        StripeRefundId = stripeRefundId.Trim();
        StripePaymentIntentId = NormalizeOptional(stripePaymentIntentId) ?? StripePaymentIntentId;
        StripeChargeId = NormalizeOptional(stripeChargeId) ?? StripeChargeId;
        LastStripeEventId = NormalizeOptional(lastStripeEventId) ?? LastStripeEventId;
        Status = PaymentRefundStatus.Pending;
        RequiresAdminAction = false;
        CanRetry = false;
        RetryBlockedReason = null;
        return Result.Success;
    }

    public Result<Success> MarkSucceeded(string? lastStripeEventId = null, DateTimeOffset? completedAtUtc = null)
    {
        Status = PaymentRefundStatus.Succeeded;
        CompletedAtUtc = completedAtUtc ?? DateTimeOffset.UtcNow;
        FailedAtUtc = null;
        FailureCode = null;
        FailureReason = null;
        SafeCustomerFailureMessage = null;
        RequiresAdminAction = false;
        CanRetry = false;
        RetryBlockedReason = null;
        LastStripeEventId = NormalizeOptional(lastStripeEventId) ?? LastStripeEventId;
        return Result.Success;
    }

    public Result<Success> MarkFailed(
        string? failureCode,
        string? failureReason,
        string? safeCustomerFailureMessage = null,
        bool requiresAdminAction = true,
        bool canRetry = false,
        string? retryBlockedReason = null,
        string? lastStripeEventId = null,
        DateTimeOffset? failedAtUtc = null)
    {
        Status = PaymentRefundStatus.Failed;
        FailureCode = NormalizeOptional(failureCode);
        FailureReason = NormalizeOptional(failureReason);
        SafeCustomerFailureMessage = string.IsNullOrWhiteSpace(safeCustomerFailureMessage)
            ? DefaultCustomerFailureMessage
            : safeCustomerFailureMessage.Trim();
        RequiresAdminAction = requiresAdminAction;
        CanRetry = canRetry;
        RetryBlockedReason = NormalizeOptional(retryBlockedReason);
        FailedAtUtc = failedAtUtc ?? DateTimeOffset.UtcNow;
        LastStripeEventId = NormalizeOptional(lastStripeEventId) ?? LastStripeEventId;
        return Result.Success;
    }

    public Result<Success> MarkCancelled(string? reason = null)
    {
        Status = PaymentRefundStatus.Cancelled;
        RequiresAdminAction = false;
        CanRetry = false;
        RetryBlockedReason = NormalizeOptional(reason);
        return Result.Success;
    }

    public Result<Success> SetRetryEligibility(bool canRetry, string? retryBlockedReason)
    {
        CanRetry = canRetry;
        RetryBlockedReason = canRetry ? null : NormalizeOptional(retryBlockedReason);
        return Result.Success;
    }

    public Result<Success> AddAdminNote(string? note)
    {
        var normalized = NormalizeOptional(note);
        if (normalized is null)
        {
            return Result.Success;
        }

        AdminNote = normalized;
        return Result.Success;
    }

    private static Guid? NormalizeGuid(Guid? id)
        => !id.HasValue || id.Value == Guid.Empty ? null : id;

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
