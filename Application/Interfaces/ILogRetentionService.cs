namespace PMIS.Application.Interfaces;

public interface ILogRetentionService
{
    Task ClearAuditLogAsync(RequestActor actor, CancellationToken cancellationToken = default);

    Task ClearNotificationsAsync(RequestActor actor, CancellationToken cancellationToken = default);

    Task RunScheduledSweepAsync(CancellationToken cancellationToken = default);
}
