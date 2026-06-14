using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Dtos;

public static class TripPolicyDtoMapper
{
    public static CancellationPolicyDto ToDto(this TripCancellation cancellation) =>
        new(
            cancellation.Actor.ToString(),
            cancellation.Reason.ToString(),
            cancellation.RefundPercent,
            cancellation.RefundAmount,
            cancellation.CurrencyCode,
            cancellation.Note,
            cancellation.CreatedAtUtc);

    public static CompensationClaimDto ToDto(this TripCompensationClaim claim) =>
        new(
            claim.Id,
            claim.TripId,
            claim.PassengerId,
            claim.Note,
            claim.EvidenceUrls.ToList(),
            claim.RequestedAmount,
            claim.CurrencyCode,
            claim.Status.ToString(),
            claim.ReviewNotes,
            claim.CreatedAtUtc,
            claim.ReviewedAtUtc);

    public static WaitingSessionDto ToDto(this TripWaitingSession session) =>
        new(
            session.Id,
            session.TripId,
            session.DriverId,
            session.StartedAtUtc,
            session.StoppedAtUtc,
            session.Minutes,
            session.EstimatedFee,
            session.IsActive,
            session.RatePerMinute,
            TripWaitingSession.GraceMinutes,
            session.BillableMinutes);
}
