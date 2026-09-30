using MongoDB.Bson;
using MongoDB.Driver;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Infrastructure.Mongo;

public class NotificationRepository(PmisMongoContext context) : INotificationRepository
{

    private readonly NewestFirstPager<Notification, DateTime> _pager =
        new(context.Notifications, n => n.CreatedDate, n => n.Id);

    public Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        => context.Notifications.InsertOneAsync(notification, cancellationToken: cancellationToken);

    public Task AddManyAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default)
        => context.Notifications.InsertManyAsync(notifications, cancellationToken: cancellationToken);

    public Task<IReadOnlyList<Notification>> GetForUserAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
        => _pager.GetPageAsync(VisibleForUser(userId), page, pageSize, cancellationToken);

    public async Task<IReadOnlyList<Notification>> GetUnreadForUserAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Notifications
            .Find(n => n.UserId == userId && !n.IsRead && !n.SuppressedByPreference)
            .SortByDescending(n => n.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public Task<long> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        => context.Notifications.CountDocumentsAsync(
            n => n.UserId == userId && !n.IsRead && !n.SuppressedByPreference,
            cancellationToken: cancellationToken);

    public Task<long> GetTotalCountAsync(Guid userId, CancellationToken cancellationToken = default)
        => context.Notifications.CountDocumentsAsync(
            VisibleForUser(userId),
            cancellationToken: cancellationToken);

    public async Task MarkReadAsync(string notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Notification>.Filter.Eq(n => n.Id, MongoDB.Bson.ObjectId.Parse(notificationId))
                   & Builders<Notification>.Filter.Eq(n => n.UserId, userId);
        var update = Builders<Notification>.Update
            .Set(n => n.IsRead, true)
            .Set(n => n.ReadDate, DateTime.UtcNow);

        await context.Notifications.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Notification>.Filter.Eq(n => n.UserId, userId)
                   & Builders<Notification>.Filter.Eq(n => n.IsRead, false);
        var update = Builders<Notification>.Update
            .Set(n => n.IsRead, true)
            .Set(n => n.ReadDate, DateTime.UtcNow);

        await context.Notifications.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
    }

    private static FilterDefinition<Notification> VisibleForUser(Guid userId)
        => Builders<Notification>.Filter.Where(n => n.UserId == userId && !n.SuppressedByPreference);

    public async Task<long> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await context.Notifications.DeleteManyAsync(
            FilterDefinition<Notification>.Empty, cancellationToken);
        return result.DeletedCount;
    }

    public async Task<long> DeleteOlderThanAsync(DateTime cutoff, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Notification>.Filter.Lt(n => n.CreatedDate, cutoff);
        var result = await context.Notifications.DeleteManyAsync(filter, cancellationToken);
        return result.DeletedCount;
    }

    public async Task<long> DeleteExcessPerUserAsync(int keepCountPerUser, CancellationToken cancellationToken = default)
    {
        var stages = new BsonDocument[]
        {
            new("$setWindowFields", new BsonDocument
            {
                { "partitionBy", "$userId" },
                { "sortBy", new BsonDocument("createdDate", -1) },
                { "output", new BsonDocument("rank", new BsonDocument("$documentNumber", new BsonDocument())) }
            }),
            new("$match", new BsonDocument("rank", new BsonDocument("$gt", keepCountPerUser))),
            new("$project", new BsonDocument("_id", 1))
        };
        var pipeline = PipelineDefinition<Notification, BsonDocument>.Create(stages);

        using var cursor = await context.Notifications.AggregateAsync(pipeline, cancellationToken: cancellationToken);
        var overflowIds = await cursor.ToListAsync(cancellationToken);

        if (overflowIds.Count == 0)
        {
            return 0;
        }

        var ids = overflowIds.Select(doc => doc["_id"].AsObjectId).ToList();
        var deleteFilter = Builders<Notification>.Filter.In(n => n.Id, ids);
        var result = await context.Notifications.DeleteManyAsync(deleteFilter, cancellationToken);
        return result.DeletedCount;
    }
}
