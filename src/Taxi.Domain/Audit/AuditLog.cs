using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Audit;

public sealed class AuditLog : Entity
{
    private AuditLog() { }

    private AuditLog(
        Guid id,
        Guid? userId,
        string action,
        string entityName,
        string entityId,
        string? oldValue,
        string? newValue)
        : base(id)
    {
        UserId = userId;
        Action = action;
        EntityName = entityName;
        EntityId = entityId;
        OldValue = oldValue;
        NewValue = newValue;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = default!;
    public string EntityName { get; private set; } = default!;
    public string EntityId { get; private set; } = default!;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static Result<AuditLog> Create(
        Guid id,
        Guid? userId,
        string action,
        string entityName,
        string entityId,
        string? oldValue,
        string? newValue)
    {
        return new AuditLog(
            id,
            userId,
            action,
            entityName,
            entityId,
            oldValue,
            newValue);
    }
}
