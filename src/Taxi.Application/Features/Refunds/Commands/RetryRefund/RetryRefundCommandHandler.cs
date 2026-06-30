using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Refunds;
using Taxi.Application.Features.Refunds.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;

namespace Taxi.Application.Features.Refunds.Commands.RetryRefund;

public sealed class RetryRefundCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IRefundLifecycleService refundLifecycleService,
    INotificationService notificationService,
    ILogger<RetryRefundCommandHandler> logger)
    : IRequestHandler<RetryRefundCommand, Result<AdminRefundDetailDto>>
{
    public async Task<Result<AdminRefundDetailDto>> Handle(RetryRefundCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var adminId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var retry = await refundLifecycleService.RetryRefundAsync(request.RefundId, adminId, request.Note, ct);
        if (retry.IsFailure)
        {
            await NotifyAdminsRetryAsync(request.RefundId, false, null, ct);
            return retry.Errors;
        }

        var refund = await context.PaymentRefunds
            .AsNoTracking()
            .FirstOrDefaultAsync(refund => refund.Id == retry.Value.Id, ct);

        if (refund is null)
        {
            return PaymentErrors.NotFound;
        }

        var details = await RefundDtoProjector.ToAdminRefundDetailsAsync(context, [refund], ct);
        await NotifyAdminsRetryAsync(refund.Id, true, refund.Status.ToString(), ct);
        return details[0];
    }

    private async Task NotifyAdminsRetryAsync(Guid refundId, bool succeeded, string? status, CancellationToken ct)
    {
        try
        {
            var refund = await context.PaymentRefunds
                .AsNoTracking()
                .FirstOrDefaultAsync(refund => refund.Id == refundId, ct);
            if (refund is null)
            {
                return;
            }

            var payment = await context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(payment => payment.Id == refund.PaymentId, ct);
            var tripReference = refund.TripId?.ToString() ?? payment?.TripId.ToString() ?? refund.PaymentId.ToString();
            if (payment is not null)
            {
                var trip = await context.Trips
                    .AsNoTracking()
                    .FirstOrDefaultAsync(trip => trip.Id == payment.TripId, ct);
                tripReference = trip?.ReferenceCode ?? tripReference;
            }

            var data = new Dictionary<string, string>
            {
                ["type"] = succeeded ? "refund_retry_succeeded" : "refund_retry_failed",
                ["refundId"] = refund.Id.ToString(),
                ["paymentId"] = refund.PaymentId.ToString(),
                ["status"] = status ?? refund.Status.ToString(),
            };
            var title = succeeded
                ? LocalizationKeys.RefundIssue.RetrySucceededAdminTitle
                : LocalizationKeys.RefundIssue.RetryFailedAdminTitle;
            var body = succeeded
                ? LocalizationKeys.RefundIssue.RetrySucceededAdminBody
                : LocalizationKeys.RefundIssue.RetryFailedAdminBody;
            object[] bodyArgs = succeeded
                ? [tripReference, status ?? refund.Status.ToString()]
                : [tripReference];

            await notificationService.SendPushNotificationToAdminsAsync(
                title,
                body,
                data,
                ct,
                bodyArgs: bodyArgs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to notify admins about refund retry {RefundId}", refundId);
        }
    }
}
