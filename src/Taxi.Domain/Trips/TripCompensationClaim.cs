using System.Text.Json;
using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Trips;

public sealed class TripCompensationClaim : AuditableEntity
{
    private TripCompensationClaim() { }

    private TripCompensationClaim(
        Guid id,
        Guid tripId,
        Guid passengerId,
        string note,
        IEnumerable<string> evidenceUrls,
        decimal requestedAmount,
        string currencyCode)
        : base(id)
    {
        TripId = tripId;
        PassengerId = passengerId;
        Note = note;
        RequestedAmount = requestedAmount;
        CurrencyCode = currencyCode;
        Status = CompensationClaimStatus.Pending;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        EvidenceUrlsJson = JsonSerializer.Serialize(evidenceUrls.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList());
    }

    public Guid TripId { get; private set; }
    public Guid PassengerId { get; private set; }
    public string Note { get; private set; } = string.Empty;
    public string EvidenceUrlsJson { get; private set; } = "[]";
    public IReadOnlyCollection<string> EvidenceUrls =>
        JsonSerializer.Deserialize<List<string>>(EvidenceUrlsJson) ?? [];
    public decimal RequestedAmount { get; private set; }
    public string CurrencyCode { get; private set; } = "EUR";
    public CompensationClaimStatus Status { get; private set; }
    public string? ReviewNotes { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }

    public static Result<TripCompensationClaim> Create(
        Guid id,
        Guid tripId,
        Guid passengerId,
        string note,
        IEnumerable<string> evidenceUrls,
        decimal requestedAmount,
        string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return TripErrors.CompensationClaimNoteRequired;
        }

        return new TripCompensationClaim(id, tripId, passengerId, note.Trim(), evidenceUrls, requestedAmount, currencyCode);
    }

    public Result<Success> Approve(string? reviewNotes)
    {
        if (Status != CompensationClaimStatus.Pending)
        {
            return TripErrors.CompensationClaimAlreadyReviewed;
        }

        Status = CompensationClaimStatus.Approved;
        ReviewNotes = reviewNotes;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        return Result.Success;
    }

    public Result<Success> Reject(string? reviewNotes)
    {
        if (Status != CompensationClaimStatus.Pending)
        {
            return TripErrors.CompensationClaimAlreadyReviewed;
        }

        Status = CompensationClaimStatus.Rejected;
        ReviewNotes = reviewNotes;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        return Result.Success;
    }
}
