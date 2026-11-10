using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Models;
using System.Data;

namespace SchoolAdmission.Api.Services;

public class AdmissionHoldService
{
    private readonly ApplicationDbContext _context;
    private readonly int _holdDurationSeconds;

    public AdmissionHoldService(
        ApplicationDbContext context,
        IOptions<HoldSettings> holdSettings)
    {
        _context = context;
        _holdDurationSeconds = holdSettings.Value.DurationSeconds;
    }

    public async Task<AdmissionHold> CreateHoldAsync(
        Guid classId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        // Lock the admission class row.
        // Other transactions requesting the same class must wait.
        var admissionClass = await _context.AdmissionClasses
            .FromSqlInterpolated(
                $"""SELECT * FROM "Classes" WHERE "Id" = {classId} FOR UPDATE""")
            .FirstOrDefaultAsync(cancellationToken);

        if (admissionClass == null)
        {
            throw new KeyNotFoundException("Admission class not found.");
        }

        var currentTime = DateTimeOffset.UtcNow;

        // Expire outdated holds for this class.
        var expiredHolds = await _context.AdmissionHolds
            .Where(h =>
                h.ClassId == classId &&
                h.Status == HoldStatus.Held &&
                h.ExpiresAt <= currentTime)
            .ToListAsync(cancellationToken);

        foreach (var hold in expiredHolds)
        {
            hold.Status = HoldStatus.Expired;
        }

        // Check whether this student already has an active
        // or confirmed application for this class.
        var existingHold = await _context.AdmissionHolds
            .FirstOrDefaultAsync(h =>
                h.ClassId == classId &&
                h.StudentId == studentId &&
                (h.Status == HoldStatus.Held ||
                 h.Status == HoldStatus.Confirmed),
                cancellationToken);

        if (existingHold != null)
        {
            throw new InvalidOperationException(
                "You already have an active or confirmed application for this class.");
        }

        // Count currently held seats.
        var heldSeats = await _context.AdmissionHolds
            .CountAsync(h =>
                h.ClassId == classId &&
                h.Status == HoldStatus.Held &&
                h.ExpiresAt > currentTime,
                cancellationToken);

        // Count confirmed admissions.
        var confirmedSeats = await _context.AdmissionHolds
            .CountAsync(h =>
                h.ClassId == classId &&
                h.Status == HoldStatus.Confirmed,
                cancellationToken);

        var availableSeats =
            admissionClass.TotalSeats - heldSeats - confirmedSeats;

        if (availableSeats <= 0)
        {
            throw new InvalidOperationException(
                "No seats are currently available for this class.");
        }

        // Create a temporary hold.
        var newHold = new AdmissionHold
        {
            ClassId = classId,
            StudentId = studentId,
            Status = HoldStatus.Held,
            CreatedAt = currentTime,
            ExpiresAt = currentTime.AddSeconds(_holdDurationSeconds)
        };

        _context.AdmissionHolds.Add(newHold);

        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return newHold;
    }

    //******************************************************************************************//
    public async Task<AdmissionHold?> GetHoldAsync(
    Guid holdId,
    string studentId,
    CancellationToken cancellationToken = default)
    {
        var hold = await _context.AdmissionHolds
            .FirstOrDefaultAsync(h =>
                h.Id == holdId &&
                h.StudentId == studentId,
                cancellationToken);

        if (hold == null)
        {
            return null;
        }

        if (hold.Status == HoldStatus.Held &&
            hold.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            hold.Status = HoldStatus.Expired;

            await _context.SaveChangesAsync(cancellationToken);
        }

        return hold;
    }

    //******************************************************************************************//
    public async Task<AdmissionHold?> ConfirmHoldAsync(
    Guid holdId,
    string studentId,
    CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        // Lock the hold record to prevent simultaneous confirmations.
        var hold = await _context.AdmissionHolds
            .FromSqlInterpolated(
                $"""SELECT * FROM "AdmissionHolds" WHERE "Id" = {holdId} FOR UPDATE""")  //FOR UPDATE prevents two simultaneous request from modifying the same record at the same time. 
            .FirstOrDefaultAsync(cancellationToken);

        // Check whether the hold exists and belongs to this student.
        if (hold == null || hold.StudentId != studentId)
        {
            return null;
        }

        // Already confirmed: return success without changing anything.
        if (hold.Status == HoldStatus.Confirmed)       //if a student confirms an already-confirmed application again, we simply return its existing record.
        {
            await transaction.CommitAsync(cancellationToken);
            return hold;
        }

        // Check expiration before confirming.
        if (hold.Status == HoldStatus.Held &&
            hold.ExpiresAt <= DateTimeOffset.UtcNow)  // if the payment confirmation arrives after expiration, the hold becomes EXPIRED, not CONFIRMED
        {
            hold.Status = HoldStatus.Expired;

            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return hold;
        }

        // Only active HELD applications can be confirmed.
        if (hold.Status != HoldStatus.Held)
        {
            await transaction.CommitAsync(cancellationToken);
            return hold;
        }

        // Confirm the admission.
        hold.Status = HoldStatus.Confirmed;
        hold.ConfirmedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return hold;
    }

    //******************************************************************************************//
    public async Task<AdmissionHold?> CancelHoldAsync(      //this method locks the hold record before modifying it, preventing simultaneous confirmation and cancelation from changing the same record. 
    Guid holdId,
    string studentId,
    CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        var hold = await _context.AdmissionHolds
            .FromSqlInterpolated(
                $"""SELECT * FROM "AdmissionHolds" WHERE "Id" = {holdId} FOR UPDATE""")
            .FirstOrDefaultAsync(cancellationToken);

        if (hold == null || hold.StudentId != studentId)
        {
            return null;
        }

        if (hold.Status == HoldStatus.Held &&
            hold.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            hold.Status = HoldStatus.Expired;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return hold;
        }

        if (hold.Status == HoldStatus.Held)
        {
            hold.Status = HoldStatus.Cancelled;

            await _context.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return hold;
    }

    //******************************************************************************************//

    public async Task<List<AdmissionHold>> GetMyHoldsAsync(
    string studentId,
    CancellationToken cancellationToken = default)
    {
        var holds = await _context.AdmissionHolds
            .Where(h => h.StudentId == studentId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;

        var expiredHolds = holds
            .Where(h =>
                h.Status == HoldStatus.Held &&
                h.ExpiresAt <= now)
            .ToList();

        foreach (var hold in expiredHolds)
        {
            hold.Status = HoldStatus.Expired;
        }

        if (expiredHolds.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return holds;
    }

}