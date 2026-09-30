using PMIS.Domain.Entities;

namespace PMIS.Application.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLogEntry>> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default);

    Task<long> CountAsync(CancellationToken cancellationToken = default);

    Task<long> DeleteAllAsync(CancellationToken cancellationToken = default);

    Task<long> DeleteOlderThanAsync(DateTime cutoff, CancellationToken cancellationToken = default);

    Task<long> DeleteExcessAsync(int keepCount, CancellationToken cancellationToken = default);
}
