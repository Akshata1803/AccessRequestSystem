namespace AccessRequestSystem.Api.DTOs.Requests;

public class AccessRequestDto
{
    public int Id { get; set; }
    public int RequesterId { get; set; }
    public string RequesterUsername { get; set; } = string.Empty;
    public int ResourceId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? DecidedById { get; set; }
    public string? DecidedByUsername { get; set; }
    public DateTime? DecidedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
