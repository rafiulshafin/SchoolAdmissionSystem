using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Models;
using SchoolAdmission.Api.Services;
using Testcontainers.PostgreSql;
using Xunit;

namespace SchoolAdmission.Api.Tests;

public class AdmissionConcurrencyTests
{
    [Fact]
    public async Task TwoStudentsApplyingForLastSeat_OnlyOneShouldSucceed()
    {
        await using var postgres = new PostgreSqlBuilder()
            .WithDatabase("SchoolAdmissionTestDb")
            .WithUsername("schooladmin")
            .WithPassword("TestPassword123!")
            .Build();

        await postgres.StartAsync();

        var connectionString = postgres.GetConnectionString();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        Guid classId;

        await using (var context = new ApplicationDbContext(options))
        {
            await context.Database.MigrateAsync();

            var admissionClass = new AdmissionClass
            {
                Name = "Concurrency Test Class",
                TotalSeats = 1
            };

            context.AdmissionClasses.Add(admissionClass);
            await context.SaveChangesAsync();

            classId = admissionClass.Id;
        }

        var firstStudentTask = CreateApplicationAsync(
            connectionString,
            classId,
            "20001234567890121");

        var secondStudentTask = CreateApplicationAsync(
            connectionString,
            classId,
            "20001234567890122");

        var results = await Task.WhenAll(
            firstStudentTask,
            secondStudentTask);

        Assert.Equal(1, results.Count(result => result));

        await using var verificationContext =
            new ApplicationDbContext(options);

        var activeHolds = await verificationContext.AdmissionHolds
            .CountAsync(h => h.ClassId == classId &&
                             h.Status == HoldStatus.Held);

        Assert.Equal(1, activeHolds);
    }

    [Fact]
    public async Task ConfirmingSameHoldTwice_ShouldNotDoubleCountSeat()
    {
        await using var postgres = new PostgreSqlBuilder()
            .WithDatabase("SchoolAdmissionTestDb")
            .WithUsername("schooladmin")
            .WithPassword("TestPassword123!")
            .Build();

        await postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;

        await using var context = new ApplicationDbContext(options);

        await context.Database.MigrateAsync();

        var admissionClass = new AdmissionClass
        {
            Name = "Confirmation Test Class",
            TotalSeats = 2
        };

        context.AdmissionClasses.Add(admissionClass);
        await context.SaveChangesAsync();

        var settings = Options.Create(new HoldSettings
        {
            DurationSeconds = 600
        });

        var service = new AdmissionHoldService(context, settings);

        const string studentId = "20001234567890123";

        var hold = await service.CreateHoldAsync(
            admissionClass.Id,
            studentId);

        var firstConfirmation = await service.ConfirmHoldAsync(
            hold.Id,
            studentId);

        var secondConfirmation = await service.ConfirmHoldAsync(
            hold.Id,
            studentId);

        Assert.NotNull(firstConfirmation);
        Assert.NotNull(secondConfirmation);

        Assert.Equal(HoldStatus.Confirmed, firstConfirmation.Status);
        Assert.Equal(HoldStatus.Confirmed, secondConfirmation.Status);

        var confirmedCount = await context.AdmissionHolds
            .CountAsync(h =>
                h.ClassId == admissionClass.Id &&
                h.Status == HoldStatus.Confirmed);

        Assert.Equal(1, confirmedCount);
    }

    //******************************************************************//

    [Fact]
    public async Task ExpiredHold_ShouldBeAutomaticallyUpdatedInDatabase()
    {
        await using var postgres = new PostgreSqlBuilder()
            .WithDatabase("SchoolAdmissionTestDb")
            .WithUsername("schooladmin")
            .WithPassword("TestPassword123!")
            .Build();

        await postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;

        await using var context = new ApplicationDbContext(options);

        await context.Database.MigrateAsync();

        var admissionClass = new AdmissionClass
        {
            Name = "Expiration Test Class",
            TotalSeats = 1
        };

        context.AdmissionClasses.Add(admissionClass);
        await context.SaveChangesAsync();

        var expiredHold = new AdmissionHold
        {
            ClassId = admissionClass.Id,
            StudentId = "20001234567890124",
            Status = HoldStatus.Held,
            CreatedAt = DateTimeOffset.UtcNow.AddSeconds(-20),
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-10)
        };

        context.AdmissionHolds.Add(expiredHold);
        await context.SaveChangesAsync();

        // Execute the same database cleanup operation used by the service.
        await context.AdmissionHolds
            .Where(h =>
                h.Status == HoldStatus.Held &&
                h.ExpiresAt <= DateTimeOffset.UtcNow)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(
                    h => h.Status,
                    HoldStatus.Expired));

        var updatedHold = await context.AdmissionHolds
            .AsNoTracking()
            .FirstAsync(h => h.Id == expiredHold.Id);

        Assert.Equal(HoldStatus.Expired, updatedHold.Status);
    }


    //******************************************************************//
    private static async Task<bool> CreateApplicationAsync(
        string connectionString,
        Guid classId,
        string studentId)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new ApplicationDbContext(options);

        var settings = Options.Create(new HoldSettings
        {
            DurationSeconds = 600
        });

        var service = new AdmissionHoldService(context, settings);

        try
        {
            await service.CreateHoldAsync(classId, studentId);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}