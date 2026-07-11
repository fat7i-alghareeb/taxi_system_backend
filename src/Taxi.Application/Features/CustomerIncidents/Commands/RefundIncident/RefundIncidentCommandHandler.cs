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
    ITripRefundSplitter refundSplitter,
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

        // All captured fare money (wallet + card). Used to size a full refund and the % for records.
        var farePayments = await context.Payments
            .Where(p => p.TripId == tripId
                && p.Status == PaymentStatus.Completed
                && (p.Kind == PaymentKind.Fare || p.Kind == PaymentKind.FareAdjustment))
            .ToListAsync(ct);
        if (farePayments.Count == 0)
        {
            return CustomerIncidentErrors.NoRefundablePayment;
        }

        var totalFare = farePayments.Sum(p => p.Amount);
        var currency = farePayments[0].Currency;
        var amount = request.Amount ?? totalFare;
        var refundPercent = totalFare > 0
            ? Math.Round(amount / totalFare * 100m, 2, MidpointRounding.AwayFromZero)
            : (decimal?)null;

        // Split across wallet-first then card so mixed / wallet-only trips refund correctly.
        var splitResult = await refundSplitter.RefundAsync(
            new TripRefundSplitRequest(
                tripId,
                amount,
                PaymentRefundSourceType.ManualIncidentRefund,
                PassengerId: incident.PassengerId,
                RefundPercent: refundPercent,
                CustomerIncidentId: incident.Id,
                RequestedByAdminId: adminId),
            ct);

        if (!splitResult.AnyCreated)
        {
            logger.LogWarning(
                "Incident refund could not be recorded for incident {IncidentId} (trip {TripId}).",
                incident.Id,
                tripId);
            return CustomerIncidentErrors.NoRefundablePayment;
        }

        if (splitResult.AnyFailed)
        {
            logger.LogWarning(
                "Incident refund failed for incident {IncidentId} (trip {TripId}); refunded={Refunded}.",
                incident.Id,
                tripId,
                splitResult.TotalRefunded);
            return CustomerIncidentErrors.RefundFailed;
        }

        incident.AddNote(
            adminId,
            $"Refund requested: {splitResult.TotalRefunded} {currency} across {splitResult.Refunds.Count} source(s).");
        await context.SaveChangesAsync(ct);

        var passengerName = await context.DomainUsers
            .AsNoTracking()
            .Where(u => u.Id == incident.PassengerId)
            .Select(u => u.Name)
            .FirstOrDefaultAsync(ct);

        return incident.ToDto(passengerName);
    }
}
