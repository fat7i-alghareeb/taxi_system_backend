using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Audit;

public static class AuditErrors
{
    public static readonly Error NotFound = Error.NotFound(
        code: "Audit.NotFound",
        description: "Audit log not found.");
}

