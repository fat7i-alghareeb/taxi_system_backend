using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Drivers;

public static class DriverDocumentErrors
{
    public static readonly Error DriverIdRequired = Error.Validation(
        code: LocalizationKeys.DriverDocument.DriverIdRequired,
        description: "Driver ID is required.");

    public static readonly Error FileUrlRequired = Error.Validation(
        code: LocalizationKeys.DriverDocument.FileUrlRequired,
        description: "File URL is required.");

    public static readonly Error RejectionNotesRequired = Error.Validation(
        code: LocalizationKeys.DriverDocument.RejectionNotesRequired,
        description: "Rejection notes are required when rejecting a document.");

    public static readonly Error NotFound = Error.NotFound(
        code: LocalizationKeys.DriverDocument.NotFound,
        description: "Driver document not found.");
}
