using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using PMIS.Domain.Entities;
using PMIS.Infrastructure.Mongo;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Tests;

public class LogRetentionRepositoryTests
{
    private static readonly DateTime BoundaryMoment = new(2026, 3, 4, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AuditLog_DeleteAllAsync_UsesAnEmptyFilter()
    {
        var harness = NewAuditLogHarness();

        await harness.Repository.DeleteAllAsync();

        Assert.Equal(new BsonDocument(), Render(Assert.Single(harness.DeleteCalls)));
    }

    [Fact]
    public async Task AuditLog_DeleteOlderThanAsync_FiltersOnTimestampStrictlyLessThanCutoff()
    {
        var harness = NewAuditLogHarness();

        await harness.Repository.DeleteOlderThanAsync(BoundaryMoment);

        var expected = new BsonDocument("timestamp", new BsonDocument("$lt", BoundaryMoment));
        Assert.Equal(expected, Render(Assert.Single(harness.DeleteCalls)));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(4, 3)]
    [InlineData(10, 9)]
    public async Task AuditLog_DeleteExcessAsync_ProbesTheKeepCountThNewestDocument(int keepCount, int expectedSkip)
    {
        var harness = NewAuditLogHarness(
            new AuditLogEntry { Id = ObjectId.GenerateNewId(), Timestamp = BoundaryMoment });

        await harness.Repository.DeleteExcessAsync(keepCount);

        Assert.Equal(expectedSkip, harness.LastFindSkip);
        Assert.Equal(1, harness.LastFindLimit);
    }

    [Fact]
    public async Task AuditLog_DeleteExcessAsync_ExcludesTheBoundaryDocumentItself()
    {
        var boundaryId = ObjectId.GenerateNewId();
        var harness = NewAuditLogHarness(
            new AuditLogEntry { Id = boundaryId, Timestamp = BoundaryMoment });

        await harness.Repository.DeleteExcessAsync(keepCount: 4);

        var expected = new BsonDocument
        {
            { "timestamp", new BsonDocument("$lte", BoundaryMoment) },
            {
                "$or", new BsonArray
                {
                    new BsonDocument("timestamp", new BsonDocument("$lt", BoundaryMoment)),
                    new BsonDocument("_id", new BsonDocument("$lt", boundaryId))
                }
            }
        };

        Assert.Equal(expected, Render(Assert.Single(harness.DeleteCalls)));
    }

    [Fact]
    public async Task AuditLog_DeleteExcessAsync_CollectionSmallerThanKeepCount_DeletesNothing()
    {
        var harness = NewAuditLogHarness(); 

        var deletedCount = await harness.Repository.DeleteExcessAsync(keepCount: 4);

        Assert.Equal(0, deletedCount);
        Assert.Empty(harness.DeleteCalls);
    }

    [Fact]
    public async Task Notifications_DeleteAllAsync_UsesAnEmptyFilter()
    {
        var harness = NewNotificationHarness();

        await harness.Repository.DeleteAllAsync();

        Assert.Equal(new BsonDocument(), Render(Assert.Single(harness.DeleteCalls)));
    }

    [Fact]
    public async Task Notifications_DeleteOlderThanAsync_FiltersOnCreatedDateStrictlyLessThanCutoff()
    {
        var harness = NewNotificationHarness();

        await harness.Repository.DeleteOlderThanAsync(BoundaryMoment);

        var expected = new BsonDocument("createdDate", new BsonDocument("$lt", BoundaryMoment));
        Assert.Equal(expected, Render(Assert.Single(harness.DeleteCalls)));
    }

    [Fact]
    public async Task Notifications_DeleteExcessPerUserAsync_PipelineRanksBySingleFieldSortBy()
    {
        var harness = NewNotificationHarness();

        await harness.Repository.DeleteExcessPerUserAsync(keepCountPerUser: 5);

        var setWindowFields = harness.AggregatePipeline![0].AsBsonDocument["$setWindowFields"].AsBsonDocument;
        Assert.Equal("$userId", setWindowFields["partitionBy"].AsString);
        Assert.Equal(new BsonDocument("createdDate", -1), setWindowFields["sortBy"].AsBsonDocument);
        Assert.Equal(
            new BsonDocument("$documentNumber", new BsonDocument()),
            setWindowFields["output"]["rank"].AsBsonDocument);
    }

    [Fact]
    public async Task Notifications_DeleteExcessPerUserAsync_MatchesRankStrictlyGreaterThanKeepCount()
    {
        var harness = NewNotificationHarness();

        await harness.Repository.DeleteExcessPerUserAsync(keepCountPerUser: 7);

        Assert.NotNull(harness.AggregatePipeline);
        Assert.Equal(3, harness.AggregatePipeline!.Count);
        var match = harness.AggregatePipeline[1].AsBsonDocument["$match"].AsBsonDocument;
        Assert.Equal(new BsonDocument("$gt", 7), match["rank"].AsBsonDocument);
        var project = harness.AggregatePipeline[2].AsBsonDocument["$project"].AsBsonDocument;
        Assert.Equal(new BsonDocument("_id", 1), project);
    }

    [Fact]
    public async Task Notifications_DeleteExcessPerUserAsync_NoOverflow_DeletesNothing()
    {
        var harness = NewNotificationHarness(); 

        var deletedCount = await harness.Repository.DeleteExcessPerUserAsync(keepCountPerUser: 100);

        Assert.Equal(0, deletedCount);
        Assert.Empty(harness.DeleteCalls);
    }

    [Fact]
    public async Task Notifications_DeleteExcessPerUserAsync_DeletesExactlyTheAggregatedOverflowIds()
    {
        var overflowIds = new[] { ObjectId.GenerateNewId(), ObjectId.GenerateNewId() };
        var harness = NewNotificationHarness(overflowIds);

        await harness.Repository.DeleteExcessPerUserAsync(keepCountPerUser: 3);

        var expected = new BsonDocument("_id", new BsonDocument("$in", new BsonArray(overflowIds)));
        Assert.Equal(expected, Render(Assert.Single(harness.DeleteCalls)));
    }

    private static BsonDocument Render(FilterDefinition<AuditLogEntry> filter)
        => filter.Render(new(BsonSerializer_SerializerFor<AuditLogEntry>(), MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry));

    private static BsonDocument Render(FilterDefinition<Notification> filter)
        => filter.Render(new(BsonSerializer_SerializerFor<Notification>(), MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry));

    private static MongoDB.Bson.Serialization.IBsonSerializer<TDocument> BsonSerializer_SerializerFor<TDocument>()
        => MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();

    private static AuditLogHarness NewAuditLogHarness(params AuditLogEntry[] boundary)
    {
        var deleteCalls = new List<FilterDefinition<AuditLogEntry>>();
        var findState = new FindState<AuditLogEntry>();
        var collection = NewAuditLogCollection(deleteCalls, findState, boundary);
        var context = NewContext(collection.Object, NewNotificationCollection([], new AggregateState<Notification>()).Object);
        return new AuditLogHarness(new AuditLogRepository(context), deleteCalls, findState);
    }

    private static NotificationHarness NewNotificationHarness(params ObjectId[] overflowIds)
    {
        var deleteCalls = new List<FilterDefinition<Notification>>();
        var aggregateState = new AggregateState<Notification> { OverflowIds = overflowIds };
        var collection = NewNotificationCollection(deleteCalls, aggregateState);
        var context = NewContext(NewAuditLogCollection([], new FindState<AuditLogEntry>()).Object, collection.Object);
        return new NotificationHarness(new NotificationRepository(context), deleteCalls, aggregateState);
    }

    private static PmisMongoContext NewContext(
        IMongoCollection<AuditLogEntry> auditLog, IMongoCollection<Notification> notifications)
    {
        var database = new Mock<IMongoDatabase>();
        database.Setup(d => d.GetCollection<AuditLogEntry>("auditLog", null)).Returns(auditLog);
        database.Setup(d => d.GetCollection<Notification>("notifications", null)).Returns(notifications);

        var client = new Mock<IMongoClient>();
        client.Setup(c => c.GetDatabase("pmis", null)).Returns(database.Object);

        return new PmisMongoContext(client.Object, Options.Create(new MongoDbSettings { DatabaseName = "pmis" }));
    }

    private static Mock<IMongoCollection<AuditLogEntry>> NewAuditLogCollection(
        List<FilterDefinition<AuditLogEntry>> deleteCalls, FindState<AuditLogEntry> findState, params AuditLogEntry[] boundary)
    {
        var collection = new Mock<IMongoCollection<AuditLogEntry>>();

        collection
            .Setup(c => c.DeleteManyAsync(
                It.IsAny<FilterDefinition<AuditLogEntry>>(), It.IsAny<CancellationToken>()))
            .Callback((FilterDefinition<AuditLogEntry> f, CancellationToken _) => deleteCalls.Add(f))
            .ReturnsAsync(new DeleteResult.Acknowledged(boundary.Length > 0 ? 1 : 0));

        collection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<AuditLogEntry>>(),
                It.IsAny<FindOptions<AuditLogEntry, AuditLogEntry>>(),
                It.IsAny<CancellationToken>()))
            .Returns((FilterDefinition<AuditLogEntry> _, FindOptions<AuditLogEntry, AuditLogEntry> options, CancellationToken _) =>
            {
                findState.Skip = options.Skip;
                findState.Limit = options.Limit;
                var results = boundary.Length > 0 ? boundary : [];
                return Task.FromResult<IAsyncCursor<AuditLogEntry>>(new SingleBatchCursor<AuditLogEntry>(results));
            });

        return collection;
    }

    private static Mock<IMongoCollection<Notification>> NewNotificationCollection(
        List<FilterDefinition<Notification>> deleteCalls, AggregateState<Notification> aggregateState)
    {
        var collection = new Mock<IMongoCollection<Notification>>();

        collection
            .Setup(c => c.DeleteManyAsync(
                It.IsAny<FilterDefinition<Notification>>(), It.IsAny<CancellationToken>()))
            .Callback((FilterDefinition<Notification> f, CancellationToken _) => deleteCalls.Add(f))
            .ReturnsAsync(new DeleteResult.Acknowledged(aggregateState.OverflowIds.Length));

        collection
            .Setup(c => c.AggregateAsync(
                It.IsAny<PipelineDefinition<Notification, BsonDocument>>(),
                It.IsAny<AggregateOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns((PipelineDefinition<Notification, BsonDocument> pipeline, AggregateOptions _, CancellationToken _) =>
            {
                aggregateState.Pipeline = pipeline.Render(new(
                    MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry.GetSerializer<Notification>(),
                    MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry)).Documents.ToList();

                var results = aggregateState.OverflowIds
                    .Select(id => new BsonDocument("_id", id))
                    .ToList();
                return Task.FromResult<IAsyncCursor<BsonDocument>>(new SingleBatchCursor<BsonDocument>(results));
            });

        return collection;
    }

    private sealed record AuditLogHarness(
        AuditLogRepository Repository, List<FilterDefinition<AuditLogEntry>> DeleteCalls, FindState<AuditLogEntry> FindState)
    {
        public int? LastFindSkip => FindState.Skip;
        public int? LastFindLimit => FindState.Limit;
    }

    private sealed record NotificationHarness(
        NotificationRepository Repository, List<FilterDefinition<Notification>> DeleteCalls, AggregateState<Notification> AggregateState)
    {
        public IReadOnlyList<BsonDocument>? AggregatePipeline => AggregateState.Pipeline;
    }

    private sealed class FindState<TDocument>
    {
        public int? Skip { get; set; }
        public int? Limit { get; set; }
    }

    private sealed class AggregateState<TDocument>
    {
        public ObjectId[] OverflowIds { get; init; } = [];
        public IReadOnlyList<BsonDocument>? Pipeline { get; set; }
    }

    private sealed class SingleBatchCursor<T>(IReadOnlyList<T> items) : IAsyncCursor<T>
    {
        private bool _consumed;

        public IEnumerable<T> Current { get; private set; } = [];

        public bool MoveNext(CancellationToken cancellationToken = default) => Advance();

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default) => Task.FromResult(Advance());

        public void Dispose() { }

        private bool Advance()
        {
            Current = _consumed ? [] : items;
            var hasBatch = !_consumed;
            _consumed = true;
            return hasBatch;
        }
    }
}
