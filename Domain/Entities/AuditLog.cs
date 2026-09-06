using Domain.Common;

namespace Domain.Entities;

public class AuditLog : BaseEntity
{
    public string EntityType { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public Guid? ActorStoreId { get; private set; }

    public string? Metadata { get; private set; }

    private AuditLog()
    {
    }

    public AuditLog(
        string entityType,
        Guid entityId,
        string action,
        Guid? actorStoreId = null,
        string? metadata = null,
        DateTime? createdAt = null)
    {
        EntityType = entityType;
        EntityId = entityId;
        Action = action;
        ActorStoreId = actorStoreId;
        Metadata = metadata;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }
}