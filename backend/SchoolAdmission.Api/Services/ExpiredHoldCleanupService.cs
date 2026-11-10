using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Services;

public class ExpiredHoldCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredHoldCleanupService> _logger;

    public ExpiredHoldCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredHoldCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Expired hold cleanup service started.");

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(5));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredHoldsAsync(stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while cleaning up expired holds.");

                await Task.Delay(
                    TimeSpan.FromSeconds(5),
                    stoppingToken);
            }
        }

        _logger.LogInformation(
            "Expired hold cleanup service stopped.");
    }

    private async Task CleanupExpiredHoldsAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var now = DateTimeOffset.UtcNow;

        var expiredCount = await context.AdmissionHolds
            .Where(h =>
                h.Status == HoldStatus.Held &&
                h.ExpiresAt <= now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    h => h.Status,
                    HoldStatus.Expired),
                cancellationToken);

        if (expiredCount > 0)
        {
            _logger.LogInformation(
                "{Count} expired admission holds were cleaned up.",
                expiredCount);
        }
    }
}



//this service:
// 1. runs automatically in the background
// 2. finds holds whose status is "Held" and the expiration date has passed
// 3. Changes their status to "Expired"
// 4. Releases their seats automatically
// 5. logs cleanup activity to the console