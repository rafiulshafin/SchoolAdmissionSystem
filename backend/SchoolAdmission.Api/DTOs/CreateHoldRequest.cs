using System.ComponentModel.DataAnnotations;

namespace SchoolAdmission.Api.DTOs;

public class CreateHoldRequest
{
    [Required]
    [RegularExpression(
        @"^[0-9]{17}$",
        ErrorMessage = "Student ID must contain exactly 17 digits."
    )]
    public string StudentId { get; set; } = string.Empty;
}