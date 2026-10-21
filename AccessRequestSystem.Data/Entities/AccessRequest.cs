namespace AccessRequestSystem.Data.Entities;

/// <summary>
/// Represents a temporary privileged access request lifecycle.
/// </summary>
public class AccessRequest
{
    public int Id { get; set; }

    // The user requesting access
    public int RequesterId { get; set; }
    public User Requester { get; set; } = null!;

    // Target resource being requested
    public int ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;

    /// <summary>
    /// Business reason justifying the temporary privileged access.
    /// </summary>
    public string Justification { get; set; } = string.Empty;

    /// <summary>
    /// Requested duration in minutes (1 to 480 minutes / 8 hours).
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// Current lifecycle state of the request.
    /// </summary>
    public AccessRequestStatus Status { get; set; } = AccessRequestStatus.Pending;

    /// <summary>
    /// Timestamp when request was submitted (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The user (Approver or Admin) who approved or denied the request.
    /// </summary>
    public int? DecidedById { get; set; }
    public User? DecidedBy { get; set; }

    /// <summary>
    /// Timestamp when the request was approved or denied (UTC).
    /// </summary>
    public DateTime? DecidedAt { get; set; }

    /// <summary>
    /// Calculated expiration timestamp (DecidedAt + DurationMinutes in UTC).
    /// Indexed to allow quick queries for expired grants by background workers.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// SHA-256 hash of the generated secret grant token.
    /// The plain token is returned ONCE to the user upon approval and never stored.
    /// </summary>
    public string? GrantTokenHash { get; set; }
}
