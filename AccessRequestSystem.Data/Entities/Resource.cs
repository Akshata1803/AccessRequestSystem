namespace AccessRequestSystem.Data.Entities;

/// <summary>
/// A target resource or privilege that users can request temporary access to.
/// </summary>
public class Resource
{
    public int Id { get; set; }

    /// <summary>
    /// Descriptive name of the resource (e.g., "Production Database Read-Only").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    // Requests associated with this resource
    public ICollection<AccessRequest> AccessRequests { get; set; } = new List<AccessRequest>();
}
