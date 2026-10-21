using System.ComponentModel.DataAnnotations;

namespace AccessRequestSystem.Api.DTOs.Requests;

public class CreateAccessRequestDto
{
    [Required]
    public int ResourceId { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 5, ErrorMessage = "Justification must be between 5 and 1000 characters.")]
    public string Justification { get; set; } = string.Empty;

    [Range(1, 480, ErrorMessage = "Duration must be between 1 and 480 minutes (max 8 hours).")]
    public int DurationMinutes { get; set; }
}
