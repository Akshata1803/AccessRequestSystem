namespace AccessRequestSystem.Data.Entities;

/// <summary>
/// System user belonging to a specific role.
/// Password is never stored in plain text; only salted PBKDF2 hash via PasswordHasher.
/// </summary>
public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }

    // Navigation property to Role
    public Role Role { get; set; } = null!;

    // Requests initiated by this user
    public ICollection<AccessRequest> RequestedAccesses { get; set; } = new List<AccessRequest>();

    // Requests approved/denied by this user (if Approver/Admin)
    public ICollection<AccessRequest> DecidedAccesses { get; set; } = new List<AccessRequest>();

    // Audit logs initiated by this user
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
