namespace AccessRequestSystem.Data.Entities;

/// <summary>
/// Represents a system role (Admin, Approver, Requester).
/// </summary>
public class Role
{
    public int Id { get; set; }

    /// <summary>
    /// Role name (e.g., "Admin", "Approver", "Requester").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    // Navigation property for users assigned to this role
    public ICollection<User> Users { get; set; } = new List<User>();
}
