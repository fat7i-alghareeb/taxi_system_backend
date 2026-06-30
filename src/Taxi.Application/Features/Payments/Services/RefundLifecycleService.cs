using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Payments.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;

namespace Taxi.Application.Features.Payments.Services;

public sealed class RefundLifecycleService(
    IAppDbContext context,
    IClientConfigProvider clientConfigProvider,
    IRefundProcessingOptionsProvider optionsProvider,
    IStripePaymentService stripe,
    INotificationService notificationService,
    ITripNotifier tripNotifier,
    ILogger<RefundLifecycleService> logger) : IRefundLifecycleService
{
    private const string GenericCustomerFailureMessage =
        LocalizationKeys.Payment.RefundCustomerFailureMessage;

    public async Task<Result<PaymentRefund>> RequestRefundAsync(RefundRequest request, CancellationToken ct = default)
    {
        var payment = await context.Payments.FirstOrDefaultAsync(payment => payment.Id == request.PaymentId, ct);
        if (payment is null)
        {
            return PaymentErrors.NotFound;
        }

        var validation = await ValidateRefundRequestAsync(payment, request, ct);
        if (validation.IsFailure)
        {
            return validation.Errors;
        }

        var amount = validation.Value.Amount;
        var idempotencyKey = BuildInitialIdempotencyKey(request, amount);
        var refundResult = PaymentRefund.Create(
            Guid.NewGuid(),
            payment.Id,
            request.SourceType,
            amount,
            payment.Currency,
            payment.Amount,
            request.RefundPercent,
            request.IsFullRefund || amount >= payment.Amount,
            request.TripId ?? payment.TripId,
            request.TripCancellationId,
            request.CustomerIncidentId,
            request.TripCompensationClaimId,
            request.RequestedByAdminId,
            request.PassengerId,
            payment.StripePaymentIntentId,
            payment.StripeChargeId,
            idempotencyKey);

        if (refundResult.IsFailure)
        {
            return refundResult.Errors;
        }

        var refund = refundResult.Value;
        refund.AddAdminNote(request.AdminNote);
        refund.MarkAttemptStarted(idempotencyKey);
        context.PaymentRefunds.Add(refund);
        await context.SaveChangesAsync(ct);
        await NotifyRefundRealtimeAsync(refund, payment, ct);

        return await ExecuteStripeRefundAsync(payment, refund, ct);
    }

    public async Task<Result<PaymentRefund>> RetryRefundAsync(
        Guid refundId,
        Guid adminId,
        string? note,
        CancellationToken ct = default)
    {
        var refund = await context.PaymentRefunds.FirstOrDefaultAsync(refund => refund.Id == refundId, ct);
        if (refund is null)
        {
            return PaymentErrors.NotFound;
        }

        if (refund.Status is not (PaymentRefundStatus.Failed
                or PaymentRefundStatus.RequiresAdminAction) ||
            !refund.CanRetry)
        {
            refund.SetRetryEligibility(false, refund.RetryBlockedReason ?? PaymentErrors.RefundRetryBlocked.Code);
            await context.SaveChangesAsync(ct);
            return PaymentErrors.RefundRetryBlocked;
        }

        var payment = await context.Payments.FirstOrDefaultAsync(payment => payment.Id == refund.PaymentId, ct);
        if (payment is null)
        {
            return PaymentErrors.NotFound;
        }

        var balance = await GetRefundableBalanceAsync(payment.Id, ct);
        if (balance.IsFailure)
        {
            return balance.Errors;
        }

        if (payment.Status == PaymentStatus.Refunded || balance.Value.AvailableRefundAmount < refund.Amount)
        {
            refund.SetRetryEligibility(false, PaymentErrors.RefundFullyRefunded.Code);
            await context.SaveChangesAsync(ct);
            return PaymentErrors.RefundFullyRefunded;
        }

        var idempotencyKey = $"refund-retry-{refund.Id:N}-{refund.AttemptCount + 1}";
        refund.AddAdminNote(note);
        refund.MarkAttemptStarted(idempotencyKey);
        await context.SaveChangesAsync(ct);
        await NotifyRefundRealtimeAsync(refund, payment, ct);

        return await ExecuteStripeRefundAsync(payment, refund, ct);
    }

    public async Task<Result<RefundableBalanceResult>> GetRefundableBalanceAsync(
        Guid paymentId,
        CancellationToken ct = default)
    {
        var payment = await context.Payments.FirstOrDefaultAsync(payment => payment.Id == paymentId, ct);
        if (payment is null)
        {
            return PaymentErrors.NotFound;
        }

        var refunds = await context.PaymentRefunds
            .Where(refund => refund.PaymentId == paymentId)
            .ToListAsync(ct);
        var totals = PaymentRefundAccounting.Calculate(payment.Amount, refunds);

        return new RefundableBalanceResult(
            payment.Id,
            payment.Amount,
            totals.SuccessfulAmount,
            totals.ReservedAmount,
            totals.FailedAmount,
            totals.AvailableAmount,
            payment.Status == PaymentStatus.Refunded || totals.IsFullyRefunded);
    }

    public async Task<Result<RefundSummaryDto?>> BuildCustomerRefundSummaryAsync(
        Guid tripId,
        Guid? paymentId = null,
        CancellationToken ct = default)
    {
        var refund = await FindLatestRefundAsync(tripId, paymentId, ct);
        if (refund is null)
        {
            return Result<RefundSummaryDto?>.SuccessOrNull(null);
        }

        var payment = await context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(payment => payment.Id == refund.PaymentId, ct);

        return ToCustomerSummary(refund, payment);
    }

    public async Task<Result<AdminRefundSummaryDto?>> BuildAdminRefundSummaryAsync(
        Guid refundId,
        Guid? tripId = null,
        CancellationToken ct = default)
    {
        var refund = await context.PaymentRefunds
            .AsNoTracking()
            .FirstOrDefaultAsync(refund => refund.Id == refundId && (!tripId.HasValue || refund.TripId == tripId), ct);
        if (refund is null)
        {
            return Result<AdminRefundSummaryDto?>.SuccessOrNull(null);
        }

        var payment = await context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(payment => payment.Id == refund.PaymentId, ct);

        return ToAdminSummary(refund, payment);
    }

    private async Task<Result<RefundValidationResult>> ValidateRefundRequestAsync(
        Payment payment,
        RefundRequest request,
        CancellationToken ct)
    {
        if (!clientConfigProvider.GetClientConfig().StripeEnabled)
        {
            return PaymentErrors.RefundStripeDisabled;
        }

        if (payment.Status != PaymentStatus.Completed ||
            string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
        {
            return PaymentErrors.RefundUnavailable;
        }

        var existingRefunds = await context.PaymentRefunds
            .Where(refund => refund.PaymentId == payment.Id)
            .ToListAsync(ct);
        var totals = PaymentRefundAccounting.Calculate(payment.Amount, existingRefunds);

        if (payment.Status == PaymentStatus.Refunded || totals.IsFullyRefunded)
        {
            return PaymentErrors.RefundFullyRefunded;
        }

        if (HasMatchingActiveRefund(existingRefunds, request))
        {
            return PaymentErrors.RefundDuplicate;
        }

        var amount = request.Amount ?? totals.AvailableAmount;
        amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0)
        {
            return PaymentErrors.InvalidAmount;
        }

        if (amount > totals.AvailableAmount)
        {
            return PaymentErrors.RefundExceedsAvailable(amount, totals.AvailableAmount);
        }

        return new RefundValidationResult(amount);
    }

    private async Task<Result<PaymentRefund>> ExecuteStripeRefundAsync(
        Payment payment,
        PaymentRefund refund,
        CancellationToken ct)
    {
        if (optionsProvider.GetOptions().ForceRefundFailure)
        {
            refund.MarkFailed(
                PaymentErrors.RefundForcedFailure.Code,
                PaymentErrors.RefundForcedFailure.Description,
                GenericCustomerFailureMessage,
                canRetry: true);
            await context.SaveChangesAsync(ct);
            await NotifyAdminsRefundFailedAsync(refund, payment, ct);
            await NotifyRefundRealtimeAsync(refund, payment, ct);
            return refund;
        }

        var stripeResult = await stripe.CreateRefundAsync(
            payment.StripePaymentIntentId!,
            refund.Amount,
            ct,
            refund.IdempotencyKey);

        if (stripeResult.IsFailure)
        {
            refund.MarkFailed(
                stripeResult.Error.Code,
                stripeResult.Error.Description,
                GenericCustomerFailureMessage,
                canRetry: true);
            await context.SaveChangesAsync(ct);
            await NotifyAdminsRefundFailedAsync(refund, payment, ct);
            await NotifyRefundRealtimeAsync(refund, payment, ct);
            return refund;
        }

        var stripeRefund = stripeResult.Value;
        refund.MarkPending(
            stripeRefund.RefundId,
            stripeRefund.PaymentIntentId,
            stripeRefund.ChargeId);

        if (string.Equals(stripeRefund.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
        {
            refund.MarkSucceeded();
        }

        await context.SaveChangesAsync(ct);
        await NotifyRefundRealtimeAsync(refund, payment, ct);
        return refund;
    }

    private async Task NotifyRefundRealtimeAsync(PaymentRefund refund, Payment payment, CancellationToken ct)
    {
        try
        {
            await tripNotifier.NotifyRefundLifecycleChangedAsync(
                refund.Id,
                payment.Id,
                refund.TripId ?? payment.TripId,
                refund.PassengerId,
                refund.Status.ToString(),
                refund.Amount,
                refund.Currency,
                refund.RequiresAdminAction,
                refund.CanRetry,
                refund.SourceType.ToString(),
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to broadcast refund lifecycle change {RefundId} for payment {PaymentId}",
                refund.Id,
                payment.Id);
        }
    }

    private async Task NotifyAdminsRefundFailedAsync(PaymentRefund refund, Payment payment, CancellationToken ct)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                ["type"] = "refund_failed",
                ["refundId"] = refund.Id.ToString(),
                ["paymentId"] = payment.Id.ToString(),
                ["tripId"] = payment.TripId.ToString(),
                ["status"] = refund.Status.ToString(),
            };

            object[] bodyArgs =
            [
                refund.Amount.ToString("0.00"),
                refund.Currency.ToUpperInvariant(),
                payment.TripId,
            ];

            await notificationService.SendPushNotificationToAdminsAsync(
                LocalizationKeys.Payment.RefundFailedAdminTitle,
                LocalizationKeys.Payment.RefundFailedAdminBody,
                data,
                ct,
                bodyArgs: bodyArgs);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to notify admins about refund failure {RefundId} for payment {PaymentId}",
                refund.Id,
                payment.Id);
        }
    }

    private async Task<PaymentRefund?> FindLatestRefundAsync(
        Guid tripId,
        Guid? paymentId,
        CancellationToken ct)
    {
        var query = context.PaymentRefunds.AsNoTracking().Where(refund => refund.TripId == tripId);
        if (paymentId.HasValue)
        {
            query = query.Where(refund => refund.PaymentId == paymentId.Value);
        }

        return await query
            .OrderByDescending(refund => refund.RequestedAtUtc)
            .ThenByDescending(refund => refund.Id)
            .FirstOrDefaultAsync(ct);
    }

    private static RefundSummaryDto ToCustomerSummary(PaymentRefund refund, Payment? payment)
    {
        var canSubmitRefundIssue = CanSubmitRefundIssue(refund.Status);

        return new RefundSummaryDto(
            refund.Status.ToString(),
            refund.Amount,
            refund.Currency,
            refund.RefundPercent,
            refund.IsFullRefund,
            payment?.Method.ToString() ?? string.Empty,
            refund.RequestedAtUtc,
            refund.CompletedAtUtc,
            refund.FailedAtUtc,
            refund.SafeCustomerFailureMessage,
            null,
            refund.RequiresAdminAction,
            canSubmitRefundIssue);
    }

    private static AdminRefundSummaryDto ToAdminSummary(PaymentRefund refund, Payment? payment)
    {
        var canSubmitRefundIssue = CanSubmitRefundIssue(refund.Status);

        return new AdminRefundSummaryDto(
            refund.Status.ToString(),
            refund.Amount,
            refund.Currency,
            refund.RefundPercent,
            refund.IsFullRefund,
            payment?.Method.ToString() ?? string.Empty,
            refund.RequestedAtUtc,
            refund.CompletedAtUtc,
            refund.FailedAtUtc,
            refund.SafeCustomerFailureMessage,
            null,
            refund.RequiresAdminAction,
            canSubmitRefundIssue,
            refund.Id,
            refund.SourceType.ToString(),
            refund.StripeRefundId,
            refund.StripePaymentIntentId,
            refund.StripeChargeId,
            refund.FailureCode,
            refund.FailureReason,
            refund.AttemptCount,
            refund.CanRetry,
            refund.RetryBlockedReason,
            refund.RequiresAdminAction);
    }

    private static bool CanSubmitRefundIssue(PaymentRefundStatus status)
        => status is PaymentRefundStatus.Failed
            or PaymentRefundStatus.RequiresAdminAction
            or PaymentRefundStatus.Succeeded;

    private static bool HasMatchingActiveRefund(IReadOnlyCollection<PaymentRefund> refunds, RefundRequest request)
        => refunds.Any(refund =>
            refund.SourceType == request.SourceType &&
            IsActiveRefundStatus(refund.Status) &&
            MatchesNullableId(refund.TripCancellationId, request.TripCancellationId) &&
            MatchesNullableId(refund.CustomerIncidentId, request.CustomerIncidentId) &&
            MatchesNullableId(refund.TripCompensationClaimId, request.TripCompensationClaimId));

    private static bool IsActiveRefundStatus(PaymentRefundStatus status)
        => status is PaymentRefundStatus.Requested
            or PaymentRefundStatus.Pending
            or PaymentRefundStatus.Retrying
            or PaymentRefundStatus.Succeeded;

    private static bool MatchesNullableId(Guid? existing, Guid? requested)
        => !requested.HasValue || existing == requested;

    private static string BuildInitialIdempotencyKey(RefundRequest request, decimal amount)
    {
        var sourceId = request.TripCancellationId
            ?? request.CustomerIncidentId
            ?? request.TripCompensationClaimId
            ?? request.TripId
            ?? request.PaymentId;

        return $"refund-{request.SourceType}-{request.PaymentId:N}-{sourceId:N}-{amount:0.00}";
    }

    private sealed record RefundValidationResult(decimal Amount);
}
