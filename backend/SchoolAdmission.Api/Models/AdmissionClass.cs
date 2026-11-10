namespace SchoolAdmission.Api.Models;

public class AdmissionClass
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public int TotalSeats { get; set; }

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    // One admission class can have many holds.
    public ICollection<AdmissionHold> AdmissionHolds { get; set; }
        = new List<AdmissionHold>();
}