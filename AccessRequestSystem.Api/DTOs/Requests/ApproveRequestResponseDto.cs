namespace AccessRequestSystem.Api.DTOs.Requests;

/// <summary>
/// Response payload returned when an access request is approved.
/// Contains the secret grant token returned ONCE in plain text.
/// </summary>
public class ApproveRequestResponseDto
{
    public int RequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DecidedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Plain-text secret grant token. The database only stores its SHA-256 hash.
    /// This value will NEVER be returned again.
    /// </summary>
    public string GrantToken { get; set; } = string.Empty;

    public string Notice { get; set; } = "Store this grant token securely now. It will never be displayed again.";
}
