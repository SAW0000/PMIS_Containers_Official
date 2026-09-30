using MongoDB.Bson;
using MongoDB.Driver;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Infrastructure.Mongo;

public class AuditLogRepository(PmisMongoContext context) : IAuditLogRepository
{
    private readonly NewestFirstPager<AuditLogEntry, DateTime> _pager =
        new(context.AuditLog, e => e.Timestamp, e => e.Id);

    public Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
        => context.AuditLog.InsertOneAsync(entry, cancellationToken: cancellationToken);

    public Task<IReadOnlyList<AuditLogEntry>> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
        => _pager.GetPageAsync(FilterDefinition<AuditLogEntry>.Empty, page, pageSize, cancellationToken);

    public Task<long> CountAsync(CancellationToken cancellationToken = default)
        => context.AuditLog.CountDocumentsAsync(FilterDefinition<AuditLogEntry>.Empty, cancellationToken: cancellationToken);

    public async Task<long> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await context.AuditLog.DeleteManyAsync(
            FilterDefinition<AuditLogEntry>.Empty, cancellationToken);
        return result.DeletedCount;
    }

    public async Task<long> DeleteOlderThanAsync(DateTime cutoff, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AuditLogEntry>.Filter.Lt(e => e.Timestamp, cutoff);
        var result = await context.AuditLog.DeleteManyAsync(filter, cancellationToken);
        return result.DeletedCount;
    }

    public async Task<long> DeleteExcessAsync(int keepCount, CancellationToken cancellationToken = default)
    {
        var boundary = await context.AuditLog
            .Find(FilterDefinition<AuditLogEntry>.Empty)
            .SortByDescending(e => e.Timestamp).ThenByDescending(e => e.Id)
            .Skip(keepCount - 1)
            .Limit(1)
            .Project<AuditLogEntry>(new BsonDocument { { "timestamp", 1 }, { "_id", 1 } })
            .FirstOrDefaultAsync(cancellationToken);

        if (boundary is null)
        {
            return 0;
        }

        var filterBuilder = Builders<AuditLogEntry>.Filter;
        var overflowFilter = filterBuilder.Lte(e => e.Timestamp, boundary.Timestamp)
            & (filterBuilder.Lt(e => e.Timestamp, boundary.Timestamp) | filterBuilder.Lt(e => e.Id, boundary.Id));

        var result = await context.AuditLog.DeleteManyAsync(overflowFilter, cancellationToken);
        return result.DeletedCount;
    }
}
