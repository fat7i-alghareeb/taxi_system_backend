using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.CustomerIncidents.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.CustomerIncidents;
using Taxi.Domain.Payments;

namespace Taxi.Application.Features.CustomerIncidents.Commands.RefundIncident;

public sealed class RefundIncidentCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IRefundLifecycleService refundLifecycle,
    ILogger<RefundIncidentCommandHandler> logger)
    : IRequestHandler<RefundIncidentCommand, Result<CustomerIncidentDto>>
{
    public async Task<Result<CustomerIncidentDto>> Handle(RefundIncidentCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var adminId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var incident = await context.CustomerIncidents.FirstOrDefaultAsync(i => i.Id == request.IncidentId, ct);
        if (incident is null)
        {
            return CustomerIncidentErrors.NotFound;
        }

        if (incident.TripId is not { } tripId)
        {
            return CustomerIncidentErrors.NoRefundablePayment;
        }

        var payment = await context.Payments.FirstOrDefaultAsync(
            p => p.TripId == tripId && p.Kind == PaymentKind.Fare, ct);

        if (payment?.Status != PaymentStatus.Completed ||
            string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
        {
            return CustomerIncidentErrors.NoRefundablePayment;
        }

        var refundPercent = request.Amount.HasValue && payment.Amount > 0
            ? Math.Round(request.Amount.Value / payment.Amount * 100m, 2, MidpointRounding.AwayFromZero)
            : (decimal?)null;
        var refundResult = await refundLifecycle.RequestRefundAsync(
            new RefundRequest(
                payment.Id,
                request.Amount,
                PaymentRefundSourceType.ManualIncidentRefund,
                refundPercent,
                !request.Amount.HasValue || request.Amount.Value >= payment.Amount,
                tripId,
                CustomerIncidentId: incident.Id,
                RequestedByAdminId: adminId,
                PassengerId: incident.PassengerId),
            ct);

        if (refundResult.IsFailure)
        {
            logger.LogWarning(
                "Failed to request tracked incident refund for PaymentIntent {PaymentIntentId} on incident {IncidentId}: {ErrorCode}",
                payment.StripePaymentIntentId,
                incident.Id,
                refundResult.Error.Code);
            return refundResult.Errors;
        }

        if (refundResult.Value.Status == PaymentRefundStatus.Failed)
        {
            logger.LogWarning(
                "Tracked incident refund {RefundId} failed immediately for PaymentIntent {PaymentIntentId} on incident {IncidentId}",
                refundResult.Value.Id,
                payment.StripePaymentIntentId,
                incident.Id);
            return CustomerIncidentErrors.RefundFailed;
        }

        var refund = refundResult.Value;
        incident.AddNote(adminId, $"Refund requested: {refund.Amount} {refund.Currency} (refundId={refund.Id}, stripeRefundId={refund.StripeRefundId}).");
        await context.SaveChangesAsync(ct);

        var passengerName = await context.DomainUsers
            .AsNoTracking()
            .Where(u => u.Id == incident.PassengerId)
            .Select(u => u.Name)
            .FirstOrDefaultAsync(ct);

        return incident.ToDto(passengerName);
    }
}
