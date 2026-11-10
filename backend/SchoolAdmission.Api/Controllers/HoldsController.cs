using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Models;
using SchoolAdmission.Api.Services;
using System.Text.RegularExpressions;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes/{classId:guid}/holds")]
public class HoldsController : ControllerBase
{
    private readonly AdmissionHoldService _holdService;

    public HoldsController(AdmissionHoldService holdService)
    {
        _holdService = holdService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateHold(
        Guid classId,
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(studentId) ||
            !System.Text.RegularExpressions.Regex.IsMatch(
                studentId,
                @"^[0-9]{17}$"))
        {
            return BadRequest(new
            {
                message = "A valid 17-digit X-Student-Id header is required."
            });
        }

        try
        {
            var hold = await _holdService.CreateHoldAsync(
                classId,
                studentId,
                cancellationToken);

            var response = new AdmissionHoldResponse
            {
                Id = hold.Id,
                ClassId = hold.ClassId,
                StudentId = hold.StudentId,
                Status = hold.Status.ToString().ToUpperInvariant(),
                CreatedAt = hold.CreatedAt,
                ExpiresAt = hold.ExpiresAt,
                ConfirmedAt = hold.ConfirmedAt,
                RemainingSeconds = Math.Max(
                    0,
                    (long)(hold.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds)
            };

            return Created(
                $"/holds/{hold.Id}",
                response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    //********************************************************************************//



    [HttpGet("/holds/{holdId:guid}")]
    public async Task<IActionResult> GetHold(
    Guid holdId,
    [FromHeader(Name = "X-Student-Id")] string? studentId,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(studentId) ||
            !System.Text.RegularExpressions.Regex.IsMatch(
                studentId,
                @"^[0-9]{17}$"))
        {
            return BadRequest(new
            {
                message = "A valid 17-digit X-Student-Id header is required."
            });
        }

        var hold = await _holdService.GetHoldAsync(
            holdId,
            studentId,
            cancellationToken);

        if (hold == null)
        {
            return NotFound(new
            {
                message = "Application not found."
            });
        }

        var response = new AdmissionHoldResponse
        {
            Id = hold.Id,
            ClassId = hold.ClassId,
            StudentId = hold.StudentId,
            Status = hold.Status.ToString().ToUpperInvariant(),
            CreatedAt = hold.CreatedAt,
            ExpiresAt = hold.ExpiresAt,
            ConfirmedAt = hold.ConfirmedAt,
            RemainingSeconds = hold.Status == Models.HoldStatus.Held
                ? Math.Max(0, (long)(hold.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds)
                : 0
        };

        return Ok(response);
    }


    //**************************************************************************************//
    [HttpPost("/holds/{holdId:guid}/confirm")]
    public async Task<IActionResult> ConfirmHold(
    Guid holdId,
    [FromHeader(Name = "X-Student-Id")] string? studentId,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(studentId) ||                    //validate the students 17 digit ID
            !System.Text.RegularExpressions.Regex.IsMatch(
                studentId,
                @"^[0-9]{17}$"))
        {
            return BadRequest(new
            {
                message = "A valid 17-digit X-Student-Id header is required."
            });
        }

        var hold = await _holdService.ConfirmHoldAsync(
            holdId,
            studentId,
            cancellationToken);

        if (hold == null)
        {
            return NotFound(new
            {
                message = "Application not found."
            });
        }

        if (hold.Status == Models.HoldStatus.Expired)
        {
            return Conflict(new
            {
                message = "This application has expired. Please apply again."
            });
        }

        if (hold.Status == Models.HoldStatus.Cancelled)
        {
            return Conflict(new
            {
                message = "This application has been cancelled."
            });
        }

        var response = new AdmissionHoldResponse
        {
            Id = hold.Id,
            ClassId = hold.ClassId,
            StudentId = hold.StudentId,
            Status = hold.Status.ToString().ToUpperInvariant(),
            CreatedAt = hold.CreatedAt,
            ExpiresAt = hold.ExpiresAt,
            ConfirmedAt = hold.ConfirmedAt,
            RemainingSeconds = 0
        };

        return Ok(response);
    }


    //**************************************************************************************//

    [HttpDelete("/holds/{holdId:guid}")]
    public async Task<IActionResult> CancelHold(
    Guid holdId,
    [FromHeader(Name = "X-Student-Id")] string? studentId,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(studentId) ||
            !Regex.IsMatch(studentId, @"^[0-9]{17}$"))
        {
            return BadRequest(new
            {
                message = "A valid 17-digit Student ID is required."
            });
        }

        var hold = await _holdService.CancelHoldAsync(
            holdId,
            studentId,
            cancellationToken);

        if (hold == null)
        {
            return NotFound(new
            {
                message = "Application not found."
            });
        }

        if (hold.Status == HoldStatus.Confirmed)
        {
            return Conflict(new
            {
                message = "A confirmed admission cannot be cancelled."
            });
        }

        var response = new AdmissionHoldResponse
        {
            Id = hold.Id,
            ClassId = hold.ClassId,
            StudentId = hold.StudentId,
            Status = hold.Status.ToString().ToUpperInvariant(),
            CreatedAt = hold.CreatedAt,
            ExpiresAt = hold.ExpiresAt,
            ConfirmedAt = hold.ConfirmedAt,
            RemainingSeconds = 0
        };

        return Ok(response);
    }


    //**************************************************************************************//

    [HttpGet("/me/holds")]
    public async Task<IActionResult> GetMyHolds(
    [FromHeader(Name = "X-Student-Id")] string? studentId,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(studentId) ||
            !Regex.IsMatch(studentId, @"^[0-9]{17}$"))
        {
            return BadRequest(new
            {
                message = "A valid 17-digit Student ID is required."
            });
        }

        var holds = await _holdService.GetMyHoldsAsync(
            studentId,
            cancellationToken);

        var response = holds.Select(hold =>
            new AdmissionHoldResponse
            {
                Id = hold.Id,
                ClassId = hold.ClassId,
                StudentId = hold.StudentId,
                Status = hold.Status.ToString().ToUpperInvariant(),
                CreatedAt = hold.CreatedAt,
                ExpiresAt = hold.ExpiresAt,
                ConfirmedAt = hold.ConfirmedAt,
                RemainingSeconds =
                    hold.Status == HoldStatus.Held
                        ? Math.Max(
                            0,
                            (long)(hold.ExpiresAt -
                                DateTimeOffset.UtcNow).TotalSeconds)
                        : 0
            });

        return Ok(response);
    }
}