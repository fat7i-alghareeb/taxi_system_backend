using Taxi.Contracts.Common;
using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Drivers;

public enum DocumentType
{
    DriversLicense,
    NationalId,
    VehicleRegistration,
    Insurance
}

public enum DocumentStatus
{
    Pending,
    Approved,
    Rejected
}

public sealed class DriverDocument : AuditableEntity
{
    private DriverDocument() { } // EF Core

    private DriverDocument(Guid id, Guid driverId, DocumentType type, string fileUrl)
        : base(id)
    {
        DriverId = driverId;
        Type = type;
        FileUrl = fileUrl;
        Status = DocumentStatus.Pending;
    }

    public Guid DriverId { get; private set; }
    public DocumentType Type { get; private set; }
    public string FileUrl { get; private set; } = default!;
    public DocumentStatus Status { get; private set; }
    public string? ReviewNotes { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static Result<DriverDocument> Create(Guid id, Guid driverId, DocumentType type, string fileUrl)
    {
        if (driverId == Guid.Empty)
        {
            return Result.Failure<DriverDocument>(Error.Validation(LocalizationKeys.DriverDocument.DriverIdRequired, "DriverId is required."));
        }

        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return Result.Failure<DriverDocument>(Error.Validation(LocalizationKeys.DriverDocument.FileUrlRequired, "FileUrl is required."));
        }

        return new DriverDocument(id, driverId, type, fileUrl);
    }

    public Result<Success> Approve(string? notes)
    {
        Status = DocumentStatus.Approved;
        ReviewNotes = notes;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        return Result.Success;
    }

    public Result<Success> Reject(string notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.DriverDocument.RejectionNotesRequired, "Rejection notes are required when rejecting a document."));
        }

        Status = DocumentStatus.Rejected;
        ReviewNotes = notes;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        return Result.Success;
    }

    public void SoftDelete() => DeletedAtUtc = DateTimeOffset.UtcNow;
}
