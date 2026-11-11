using Microsoft.EntityFrameworkCore;
using SchoolAdmissionSystem.Api.Data;
using SchoolAdmissionSystem.Api.Models.Enums;

namespace SchoolAdmissionSystem.Api.Services;

public class HoldCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HoldCleanupBackgroundService> _logger;
    private readonly TimeSpan _checkInterval;

    public HoldCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<HoldCleanupBackgroundService> logger,
        TimeSpan? checkInterval = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _checkInterval = checkInterval ?? TimeSpan.FromSeconds(5);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HoldCleanupBackgroundService is starting with check interval {Interval}.", _checkInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during hold cleanup execution.");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("HoldCleanupBackgroundService has stopped.");
    }

    public async Task<int> DoCleanupAsync(CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTimeOffset.UtcNow;
        var expiredHolds = await context.Holds
            .Where(h => h.Status == HoldStatus.HELD && h.ExpiresAt <= now)
            .ToListAsync(ct);

        if (expiredHolds.Count == 0)
        {
            return 0;
        }

        foreach (var hold in expiredHolds)
        {
            hold.Status = HoldStatus.EXPIRED;
        }

        await context.SaveChangesAsync(ct);
        _logger.LogInformation("Marked {Count} expired holds as EXPIRED.", expiredHolds.Count);

        return expiredHolds.Count;
    }
}
