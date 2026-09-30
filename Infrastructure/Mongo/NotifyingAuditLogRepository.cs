using Microsoft.Extensions.Logging;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;

namespace PMIS.Infrastructure.Mongo;

public class NotifyingAuditLogRepository(
    AuditLogRepository inner,
    INotificationService notificationService,
    ILogger<NotifyingAuditLogRepository> logger) : IAuditLogRepository
{
    public async Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        await inner.AddAsync(entry, cancellationToken);

        try
        {
            await notificationService.NotifyNewAuditLogEntryAsync(entry, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Audit entry {Action} was written but notifying administrators failed.", entry.Action);
        }
    }

    public Task<IReadOnlyList<AuditLogEntry>> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
        => inner.GetPagedAsync(page, pageSize, cancellationToken);

    public Task<long> CountAsync(CancellationToken cancellationToken = default)
        => inner.CountAsync(cancellationToken);

    public Task<long> DeleteAllAsync(CancellationToken cancellationToken = default)
        => inner.DeleteAllAsync(cancellationToken);

    public Task<long> DeleteOlderThanAsync(DateTime cutoff, CancellationToken cancellationToken = default)
        => inner.DeleteOlderThanAsync(cutoff, cancellationToken);

    public Task<long> DeleteExcessAsync(int keepCount, CancellationToken cancellationToken = default)
        => inner.DeleteExcessAsync(keepCount, cancellationToken);
}
