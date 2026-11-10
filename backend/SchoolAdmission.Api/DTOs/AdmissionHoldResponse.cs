using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.DTOs;

public class AdmissionHoldResponse
{
    public Guid Id { get; set; }

    public Guid ClassId { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? ConfirmedAt { get; set; }

    public long RemainingSeconds { get; set; }
}