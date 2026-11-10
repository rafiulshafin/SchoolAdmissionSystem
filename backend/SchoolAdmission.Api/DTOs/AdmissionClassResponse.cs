namespace SchoolAdmission.Api.DTOs;

public class AdmissionClassResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}