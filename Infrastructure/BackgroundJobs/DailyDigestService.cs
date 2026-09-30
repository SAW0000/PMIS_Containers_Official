using PMIS.Application.Interfaces;

namespace PMIS.Infrastructure.BackgroundJobs;

public class DailyDigestService(
    IServiceScopeFactory scopeFactory,
    ILogger<DailyDigestService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Daily digest service started; interval {Interval}.", Interval);
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
            await RunOnceAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Daily digest run failed; skipping this cycle.");
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var requestQueryService = scope.ServiceProvider.GetRequiredService<IRequestQueryService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var overdueCount = await requestQueryService.GetOverdueRequestCountAsync(cancellationToken);
        var paymentNeededCount = await requestQueryService.GetPaymentConfirmationNeededCountAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Daily digest: {OverdueCount} overdue, {PaymentNeededCount} awaiting payment confirmation.",
                overdueCount, paymentNeededCount);
        }

        if (overdueCount > 0)
        {
            await notificationService.NotifyOverdueRequestsDigestAsync(overdueCount, cancellationToken);
        }

        if (paymentNeededCount > 0)
        {
            await notificationService.NotifyPaymentConfirmationNeededDigestAsync(paymentNeededCount, cancellationToken);
        }
    }
}
