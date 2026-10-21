namespace AccessRequestSystem.Admin.Models;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresInMinutes { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class PendingRequestItem
{
    public int Id { get; set; }
    public string RequesterUsername { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ApproveResponse
{
    public int RequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DecidedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string GrantToken { get; set; } = string.Empty;
    public string Notice { get; set; } = string.Empty;
}

public class AuditLogItem
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string? ActorUsername { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Details { get; set; } = string.Empty;
}

public class PagedAuditResult
{
    public List<AuditLogItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
