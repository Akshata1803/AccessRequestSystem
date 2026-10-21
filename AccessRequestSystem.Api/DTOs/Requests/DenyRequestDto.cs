using System.ComponentModel.DataAnnotations;

namespace AccessRequestSystem.Api.DTOs.Requests;

public class DenyRequestDto
{
    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
    public string? Reason { get; set; }
}
