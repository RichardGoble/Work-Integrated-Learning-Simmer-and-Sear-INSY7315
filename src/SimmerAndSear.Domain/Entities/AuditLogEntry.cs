using SimmerAndSear.Domain.Abstractions;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Domain.Entities;

/// <summary>
/// An append-only record of who changed what, and when. There is deliberately no
/// mutating behaviour on this type, no update or delete method on its repository,
/// and a $jsonSchema validator guarding the collection.
/// </summary>
public sealed class AuditLogEntry : IEntity
{
    public string Id { get; set; } = string.Empty;

    public string ActorId { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public RoleName ActorRole { get; set; }

    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? IpAddress { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Factory Method - the only supported way to construct an audit entry, so
    /// that the timestamp and required fields can never be omitted by a caller.
    /// </summary>
    public static AuditLogEntry Create(
        string actorId,
        string actorName,
        RoleName actorRole,
        AuditAction action,
        string entityType,
        string? entityId,
        string summary,
        string? ipAddress = null) => new()
        {
            ActorId = actorId,
            ActorName = actorName,
            ActorRole = actorRole,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Summary = summary,
            IpAddress = ipAddress,
            OccurredAt = DateTime.UtcNow
        };
}
