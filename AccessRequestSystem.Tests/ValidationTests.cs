using System.ComponentModel.DataAnnotations;
using AccessRequestSystem.Api.DTOs.Requests;

namespace AccessRequestSystem.Tests;

public class ValidationTests
{
    [Theory]
    [InlineData(1, true)]
    [InlineData(60, true)]
    [InlineData(480, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(481, false)]
    [InlineData(1000, false)]
    public void CreateAccessRequestDto_DurationMinutes_EnforcesRangeBetween1And480(int durationMinutes, bool shouldBeValid)
    {
        // Arrange
        var dto = new CreateAccessRequestDto
        {
            ResourceId = 1,
            Justification = "Valid justification text explaining the business need.",
            DurationMinutes = durationMinutes
        };

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(dto);

        // Act
        var isValid = Validator.TryValidateObject(dto, context, validationResults, validateAllProperties: true);

        // Assert
        Assert.Equal(shouldBeValid, isValid);

        if (!shouldBeValid)
        {
            Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreateAccessRequestDto.DurationMinutes)));
        }
    }
}
