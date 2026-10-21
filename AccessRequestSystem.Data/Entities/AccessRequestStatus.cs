namespace AccessRequestSystem.Data.Entities;

/// <summary>
/// Lifecycle statuses for an access request.
/// Stored as string in the database for human-readable auditability.
/// </summary>
public enum AccessRequestStatus
{
    Pending,
    Approved,
    Denied,
    Expired,
    Revoked
}
