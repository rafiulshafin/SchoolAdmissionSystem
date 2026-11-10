using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ClassesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // POST: /classes
    [HttpPost]
    public async Task<IActionResult> CreateClass(
        [FromBody] CreateAdmissionClassRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Class name is required."
            });
        }

        var admissionClass = new AdmissionClass
        {
            Name = request.Name.Trim(),
            TotalSeats = request.TotalSeats
        };

        _context.AdmissionClasses.Add(admissionClass);

        await _context.SaveChangesAsync();

        var response = new AdmissionClassResponse
        {
            Id = admissionClass.Id,
            Name = admissionClass.Name,
            TotalSeats = admissionClass.TotalSeats,
            CreatedAt = admissionClass.CreatedAt
        };

        return Created(
            $"/classes/{admissionClass.Id}",
            response
        );
    }

    // GET: /classes
    [HttpGet]
    public async Task<IActionResult> GetAllClasses()
    {
        var currentTime = DateTimeOffset.UtcNow;

        var classes = await _context.AdmissionClasses
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .Select(c => new AdmissionClassDetailsResponse
            {
                Id = c.Id,
                Name = c.Name,
                TotalSeats = c.TotalSeats,
                CreatedAt = c.CreatedAt,

                HeldSeats = c.AdmissionHolds.Count(h =>
                    h.Status == HoldStatus.Held &&
                    h.ExpiresAt > currentTime),

                ConfirmedSeats = c.AdmissionHolds.Count(h =>
                    h.Status == HoldStatus.Confirmed),

                AvailableSeats = c.TotalSeats -
                    c.AdmissionHolds.Count(h =>
                        h.Status == HoldStatus.Held &&
                        h.ExpiresAt > currentTime) -
                    c.AdmissionHolds.Count(h =>
                        h.Status == HoldStatus.Confirmed)
            })
            .ToListAsync();

        return Ok(classes);
    }

    // GET: /classes/{classId}
    [HttpGet("{classId:guid}")]
    public async Task<IActionResult> GetClassById(Guid classId)
    {
        var currentTime = DateTimeOffset.UtcNow;

        var admissionClass = await _context.AdmissionClasses
            .AsNoTracking()
            .Where(c => c.Id == classId)
            .Select(c => new AdmissionClassDetailsResponse
            {
                Id = c.Id,
                Name = c.Name,
                TotalSeats = c.TotalSeats,
                CreatedAt = c.CreatedAt,

                HeldSeats = c.AdmissionHolds.Count(h =>
                    h.Status == HoldStatus.Held &&
                    h.ExpiresAt > currentTime),

                ConfirmedSeats = c.AdmissionHolds.Count(h =>
                    h.Status == HoldStatus.Confirmed),

                AvailableSeats = c.TotalSeats -
                    c.AdmissionHolds.Count(h =>
                        h.Status == HoldStatus.Held &&
                        h.ExpiresAt > currentTime) -
                    c.AdmissionHolds.Count(h =>
                        h.Status == HoldStatus.Confirmed)
            })
            .FirstOrDefaultAsync();

        if (admissionClass == null)
        {
            return NotFound(new
            {
                message = "Admission class not found."
            });
        }

        return Ok(admissionClass);
    }
}