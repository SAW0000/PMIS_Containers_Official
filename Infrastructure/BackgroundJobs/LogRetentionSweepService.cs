using PMIS.Application.Interfaces;

namespace PMIS.Infrastructure.BackgroundJobs;

public class LogRetentionSweepService(
    IServiceScopeFactory scopeFactory,
    ILogger<LogRetentionSweepService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Log retention sweep service started; interval {Interval}.", Interval);
        }

        using var timer = new PeriodicTimer(Interval);

        await RunSafelyAsync(stoppingToken);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunSafelyAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RunSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var logRetentionService = scope.ServiceProvider.GetRequiredService<ILogRetentionService>();
            await logRetentionService.RunScheduledSweepAsync(cancellationToken);
            logger.LogInformation("Log retention sweep completed.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Log retention sweep failed; skipping this cycle.");
        }
    }
}
