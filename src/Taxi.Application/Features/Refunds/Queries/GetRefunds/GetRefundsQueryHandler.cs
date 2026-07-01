using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Refunds;
using Taxi.Application.Features.Refunds.Dtos;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Refunds.Queries.GetRefunds;

public sealed class GetRefundsQueryHandler(IAppDbContext context, IClientConfigProvider? clientConfigProvider = null)
    : IRequestHandler<GetRefundsQuery, Result<PagedResult<AdminRefundDetailDto>>>
{
    public async Task<Result<PagedResult<AdminRefundDetailDto>>> Handle(GetRefundsQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var query = context.PaymentRefunds.AsNoTracking();

        if (Enum.TryParse<PaymentRefundStatus>(request.Status, ignoreCase: true, out var status))
        {
            query = query.Where(refund => refund.Status == status);
        }

        if (Enum.TryParse<PaymentRefundSourceType>(request.SourceType, ignoreCase: true, out var sourceType))
        {
            query = query.Where(refund => refund.SourceType == sourceType);
        }

        if (request.RequiresAdminAction.HasValue)
        {
            query = query.Where(refund => refund.RequiresAdminAction == request.RequiresAdminAction.Value);
        }

        if (request.FromUtc.HasValue)
        {
            query = query.Where(refund => refund.RequestedAtUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query = query.Where(refund => refund.RequestedAtUtc <= request.ToUtc.Value);
        }

        if (request.TripId is { } tripId && tripId != Guid.Empty)
        {
            query = query.Where(refund => refund.TripId == tripId);
        }

        if (request.PassengerId is { } passengerId && passengerId != Guid.Empty)
        {
            query = query.Where(refund => refund.PassengerId == passengerId);
        }

        if (request.MinAmount.HasValue)
        {
            query = query.Where(refund => refund.Amount >= request.MinAmount.Value);
        }

        if (request.MaxAmount.HasValue)
        {
            query = query.Where(refund => refund.Amount <= request.MaxAmount.Value);
        }

        if (Enum.TryParse<PaymentMethod>(request.PaymentMethod, ignoreCase: true, out var paymentMethod))
        {
            var paymentIds = await context.Payments
                .AsNoTracking()
                .Where(payment => payment.Method == paymentMethod)
                .Select(payment => payment.Id)
                .ToListAsync(ct);
            query = query.Where(refund => paymentIds.Contains(refund.PaymentId));
        }

        var refundRows = await query.ToListAsync(ct);

        var trackedCancellationIds = await context.PaymentRefunds
            .AsNoTracking()
            .Where(refund => refund.TripCancellationId.HasValue)
            .Select(refund => refund.TripCancellationId!.Value)
            .ToListAsync(ct);

        var cancellationQuery = context.TripCancellations
            .AsNoTracking()
            .Where(cancellation => cancellation.RefundAmount > 0m && !trackedCancellationIds.Contains(cancellation.Id));

        if (Enum.TryParse<PaymentRefundStatus>(request.Status, ignoreCase: true, out var manualStatus) &&
            manualStatus != PaymentRefundStatus.Failed)
        {
            cancellationQuery = cancellationQuery.Where(_ => false);
        }

        if (Enum.TryParse<PaymentRefundSourceType>(request.SourceType, ignoreCase: true, out var manualSourceType))
        {
            cancellationQuery = manualSourceType switch
            {
                PaymentRefundSourceType.AirportWaitCancellation =>
                    cancellationQuery.Where(cancellation => cancellation.Reason == CancellationReason.AirportWaitDeclined),
                PaymentRefundSourceType.AdminCancellation =>
                    cancellationQuery.Where(cancellation =>
                        cancellation.Actor == CancellationActor.Admin &&
                        cancellation.Reason != CancellationReason.AirportWaitDeclined),
                PaymentRefundSourceType.DriverCancellation =>
                    cancellationQuery.Where(cancellation =>
                        cancellation.Actor == CancellationActor.Driver &&
                        cancellation.Reason != CancellationReason.AirportWaitDeclined),
                PaymentRefundSourceType.PassengerCancellation =>
                    cancellationQuery.Where(cancellation =>
                        cancellation.Actor == CancellationActor.Passenger &&
                        cancellation.Reason != CancellationReason.AirportWaitDeclined),
                _ => cancellationQuery.Where(_ => false),
            };
        }

        if (request.RequiresAdminAction == false)
        {
            cancellationQuery = cancellationQuery.Where(_ => false);
        }

        if (request.FromUtc.HasValue)
        {
            cancellationQuery = cancellationQuery.Where(cancellation => cancellation.CreatedAtUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            cancellationQuery = cancellationQuery.Where(cancellation => cancellation.CreatedAtUtc <= request.ToUtc.Value);
        }

        if (request.TripId is { } manualTripId && manualTripId != Guid.Empty)
        {
            cancellationQuery = cancellationQuery.Where(cancellation => cancellation.TripId == manualTripId);
        }

        if (request.PassengerId is { } manualPassengerId && manualPassengerId != Guid.Empty)
        {
            var passengerTripIds = context.Trips
                .AsNoTracking()
                .Where(trip => trip.PassengerId == manualPassengerId)
                .Select(trip => trip.Id);
            cancellationQuery = cancellationQuery.Where(cancellation => passengerTripIds.Contains(cancellation.TripId));
        }

        if (request.MinAmount.HasValue)
        {
            cancellationQuery = cancellationQuery.Where(cancellation => cancellation.RefundAmount >= request.MinAmount.Value);
        }

        if (request.MaxAmount.HasValue)
        {
            cancellationQuery = cancellationQuery.Where(cancellation => cancellation.RefundAmount <= request.MaxAmount.Value);
        }

        if (Enum.TryParse<PaymentMethod>(request.PaymentMethod, ignoreCase: true, out var manualPaymentMethod))
        {
            var paymentTripIds = context.Payments
                .AsNoTracking()
                .Where(payment => payment.Method == manualPaymentMethod)
                .Select(payment => payment.TripId);
            cancellationQuery = cancellationQuery.Where(cancellation => paymentTripIds.Contains(cancellation.TripId));
        }

        var manualRows = await cancellationQuery.ToListAsync(ct);
        var refundItems = await RefundDtoProjector.ToAdminRefundDetailsAsync(
            context,
            refundRows,
            ct,
            ForceRetryForFailedRefunds());
        var manualItems = await RefundDtoProjector.ToManualCancellationRefundDetailsAsync(context, manualRows, ct);
        var ordered = refundItems
            .Concat(manualItems)
            .OrderByDescending(item => item.RequiresAdminAction)
            .ThenByDescending(item => item.Status == PaymentRefundStatus.Failed.ToString())
            .ThenByDescending(item => item.RequestedAtUtc)
            .ToList();

        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<AdminRefundDetailDto>(items, ordered.Count, page, pageSize);
    }

    private bool ForceRetryForFailedRefunds()
        => clientConfigProvider?.GetClientConfig().StripeEnabled == false;
}
