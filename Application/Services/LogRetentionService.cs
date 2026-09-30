using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Infrastructure.Persistence.Configurations;

namespace PMIS.Application.Services;

public class LogRetentionService(
    IAuditLogRepository auditLogRepository,
    INotificationRepository notificationRepository,
    ISystemSettingsService systemSettingsService) : ILogRetentionService
{
    public async Task ClearAuditLogAsync(RequestActor actor, CancellationToken cancellationToken = default)
    {
        await auditLogRepository.AddAsync(BuildEntry("AuditLogCleared", actor), cancellationToken);
        await auditLogRepository.DeleteAllAsync(cancellationToken);
    }

    public async Task ClearNotificationsAsync(RequestActor actor, CancellationToken cancellationToken = default)
    {
        await auditLogRepository.AddAsync(BuildEntry("NotificationLogCleared", actor), cancellationToken);
        await notificationRepository.DeleteAllAsync(cancellationToken);
    }

    public async Task RunScheduledSweepAsync(CancellationToken cancellationToken = default)
    {
        var settings = await systemSettingsService.GetAsync(cancellationToken);
        var now = DateTime.UtcNow;

        if (settings.AuditLogRetentionDays > 0)
        {
            await auditLogRepository.DeleteOlderThanAsync(now.AddDays(-settings.AuditLogRetentionDays), cancellationToken);
        }

        if (settings.NotificationRetentionDays > 0)
        {
            await notificationRepository.DeleteOlderThanAsync(now.AddDays(-settings.NotificationRetentionDays), cancellationToken);
        }

        if (settings.AuditLogMaxCount > 0)
        {
            await auditLogRepository.DeleteExcessAsync(settings.AuditLogMaxCount, cancellationToken);
        }

        if (settings.NotificationMaxCount > 0)
        {
            await notificationRepository.DeleteExcessPerUserAsync(settings.NotificationMaxCount, cancellationToken);
        }
    }

    private static AuditLogEntry BuildEntry(string action, RequestActor actor) => new()
    {
        EntityType = "Settings",
        EntityId = SystemSettingsConfiguration.SingletonId,
        Action = action,
        PerformedBy = new AuditLogActor
        {
            UserId = actor.UserId,
            FullNameSnapshot = actor.FullName,
            Role = actor.Role.ToString()
        },
        Timestamp = DateTime.UtcNow,
        IpAddress = actor.IpAddress
    };
}
