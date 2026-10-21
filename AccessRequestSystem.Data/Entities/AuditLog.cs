namespace AccessRequestSystem.Data.Entities;

/// <summary>
/// Immutable audit log trail capturing every state change and critical action.
/// </summary>
public class AuditLog
{
    public int Id { get; set; }

    /// <summary>
    /// Event timestamp in UTC.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who performed the action. Null indicates an automated system process (e.g., worker).
    /// </summary>
    public int? ActorUserId { get; set; }
    public User? ActorUser { get; set; }

    /// <summary>
    /// Action verb, e.g. "CreateRequest", "ApproveRequest", "DenyRequest", "ExpireGrant".
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity affected (e.g., "AccessRequest").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Primary key ID of the affected entity.
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// Human-readable explanation and metadata of what changed.
    /// </summary>
    public string Details { get; set; } = string.Empty;
}
