using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using PMIS.Domain.Entities;
using PMIS.Infrastructure.Mongo;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Tests;

public class MongoKeysetPagingTests
{
    private static readonly DateTime BoundaryMoment = new(2026, 3, 4, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AuditLog_FirstPage_IssuesOneQueryAndNeverSkips()
    {
        var harness = NewAuditLogHarness();

        await harness.Repository.GetPagedAsync(page: 1, pageSize: 10);

        var call = Assert.Single(harness.Calls);
        Assert.Null(call.Options.Skip);
        Assert.Equal(10, call.Options.Limit);
        Assert.Equal(new BsonDocument(), Render(call.Filter));
    }

    [Fact]
    public async Task AuditLog_FirstPage_SortsByTimestampThenIdDescending()
    {
        var harness = NewAuditLogHarness();

        await harness.Repository.GetPagedAsync(page: 1, pageSize: 10);

        Assert.Equal(
            new BsonDocument { { "timestamp", -1 }, { "_id", -1 } },
            Render(Assert.Single(harness.Calls).Options.Sort!));
    }

    [Fact]
    public async Task AuditLog_SecondPage_ProbesTheBoundaryThenRangeFetches()
    {
        var boundaryId = ObjectId.GenerateNewId();
        var harness = NewAuditLogHarness(
            new AuditLogEntry { Id = boundaryId, Timestamp = BoundaryMoment });

        await harness.Repository.GetPagedAsync(page: 2, pageSize: 10);

        Assert.Equal(2, harness.Calls.Count);

        var probe = harness.Calls[0];
        Assert.Equal(9, probe.Options.Skip);
        Assert.Equal(1, probe.Options.Limit);

        var page = harness.Calls[1];
        Assert.Null(page.Options.Skip);
        Assert.Equal(10, page.Options.Limit);
    }

    [Fact]
    public async Task AuditLog_BoundaryProbe_ProjectsOnlyTheSortKeys()
    {
        var harness = NewAuditLogHarness(
            new AuditLogEntry { Id = ObjectId.GenerateNewId(), Timestamp = BoundaryMoment });

        await harness.Repository.GetPagedAsync(page: 2, pageSize: 10);

        Assert.Equal(
            new BsonDocument { { "timestamp", 1 }, { "_id", 1 } },
            Render(harness.Calls[0].Options.Projection!));
    }

    [Fact]
    public async Task AuditLog_SecondPage_RangeFilterBreaksTimestampTiesById()
    {
        var boundaryId = ObjectId.GenerateNewId();
        var harness = NewAuditLogHarness(
            new AuditLogEntry { Id = boundaryId, Timestamp = BoundaryMoment });

        await harness.Repository.GetPagedAsync(page: 2, pageSize: 10);

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

        Assert.Equal(expected, Render(harness.Calls[1].Filter));
    }

    [Theory]
    [InlineData(2, 10, 9)]
    [InlineData(3, 10, 19)]
    [InlineData(5, 20, 79)]
    [InlineData(11, 1, 9)]
    public async Task AuditLog_BoundaryProbe_SkipsToTheRowBeforeThePage(int page, int pageSize, int expectedSkip)
    {
        var harness = NewAuditLogHarness(
            new AuditLogEntry { Id = ObjectId.GenerateNewId(), Timestamp = BoundaryMoment });

        await harness.Repository.GetPagedAsync(page, pageSize);

        Assert.Equal(expectedSkip, harness.Calls[0].Options.Skip);
    }

    [Fact]
    public async Task AuditLog_PageBeyondTheEnd_ReturnsEmptyWithoutASecondQuery()
    {
        var harness = NewAuditLogHarness();

        var entries = await harness.Repository.GetPagedAsync(page: 900, pageSize: 10);

        Assert.Empty(entries);
        Assert.Single(harness.Calls);
    }

    [Fact]
    public async Task AuditLog_AbsurdPageNumber_StillSkipsForward()
    {
        var harness = NewAuditLogHarness();

        await harness.Repository.GetPagedAsync(int.MaxValue, pageSize: 100);

        Assert.True(harness.Calls[0].Options.Skip >= 0);
    }

    [Fact]
    public async Task Notifications_FirstPage_IssuesOneQueryAndNeverSkips()
    {
        var harness = NewNotificationHarness();

        await harness.Repository.GetForUserAsync(Guid.NewGuid(), page: 1, pageSize: 10);

        var call = Assert.Single(harness.Calls);
        Assert.Null(call.Options.Skip);
        Assert.Equal(10, call.Options.Limit);
        Assert.Equal(
            new BsonDocument { { "createdDate", -1 }, { "_id", -1 } },
            Render(call.Options.Sort!));
    }

    [Fact]
    public async Task Notifications_SecondPage_KeepsTheSuppressionFilterInBothPhases()
    {
        var userId = Guid.NewGuid();
        var harness = NewNotificationHarness(
            new Notification { Id = ObjectId.GenerateNewId(), CreatedDate = BoundaryMoment });

        await harness.Repository.GetForUserAsync(userId, page: 2, pageSize: 10);

        Assert.Equal(2, harness.Calls.Count);

        var visibleForUser = new BsonDocument
        {
            { "userId", userId.ToString() },
            { "suppressedByPreference", new BsonDocument("$ne", true) }
        };

        Assert.Equal(visibleForUser, Render(harness.Calls[0].Filter));

        var page = Render(harness.Calls[1].Filter);
        Assert.Equal(userId.ToString(), page["userId"].AsString);
        Assert.Equal(BoundaryMoment, page["createdDate"]["$lte"].ToUniversalTime());
    }

    [Fact]
    public async Task Notifications_BoundaryProbe_ProjectsOnlyTheSortKeys()
    {
        var harness = NewNotificationHarness(
            new Notification { Id = ObjectId.GenerateNewId(), CreatedDate = BoundaryMoment });

        await harness.Repository.GetForUserAsync(Guid.NewGuid(), page: 3, pageSize: 25);

        Assert.Equal(
            new BsonDocument { { "createdDate", 1 }, { "_id", 1 } },
            Render(harness.Calls[0].Options.Projection!));
        Assert.Equal(49, harness.Calls[0].Options.Skip);
    }

    [Fact]
    public async Task Notifications_TotalCount_CountsTheSameSetThePageReads()
    {
        var userId = Guid.NewGuid();
        var harness = NewNotificationHarness();
        FilterDefinition<Notification>? counted = null;

        harness.Collection
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Notification>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback((FilterDefinition<Notification> f, CountOptions _, CancellationToken __) => counted = f)
            .ReturnsAsync(0L);

        await harness.Repository.GetTotalCountAsync(userId);
        await harness.Repository.GetForUserAsync(userId, page: 1, pageSize: 10);

        Assert.Equal(Render(harness.Calls[0].Filter), Render(counted!));
    }

    private static BsonDocument Render<TDocument>(FilterDefinition<TDocument> filter)
        => filter.Render(RenderArgsFor<TDocument>());

    private static BsonDocument Render<TDocument>(SortDefinition<TDocument> sort)
        => sort.Render(RenderArgsFor<TDocument>());

    private static BsonDocument Render<TDocument>(ProjectionDefinition<TDocument, TDocument> projection)
        => projection.Render(RenderArgsFor<TDocument>()).Document;

    private static RenderArgs<TDocument> RenderArgsFor<TDocument>()
        => new(BsonSerializer.SerializerRegistry.GetSerializer<TDocument>(), BsonSerializer.SerializerRegistry);

    private static Harness<AuditLogEntry, AuditLogRepository> NewAuditLogHarness(params AuditLogEntry[] boundary)
    {
        var calls = new List<RecordedFind<AuditLogEntry>>();
        var collection = NewCollection(calls, boundary);
        var context = NewContext(collection.Object, NewCollection(new List<RecordedFind<Notification>>()).Object);
        return new Harness<AuditLogEntry, AuditLogRepository>(collection, calls, new AuditLogRepository(context));
    }

    private static Harness<Notification, NotificationRepository> NewNotificationHarness(params Notification[] boundary)
    {
        var calls = new List<RecordedFind<Notification>>();
        var collection = NewCollection(calls, boundary);
        var context = NewContext(NewCollection(new List<RecordedFind<AuditLogEntry>>()).Object, collection.Object);
        return new Harness<Notification, NotificationRepository>(collection, calls, new NotificationRepository(context));
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

    private static Mock<IMongoCollection<TDocument>> NewCollection<TDocument>(
        List<RecordedFind<TDocument>> calls, params TDocument[] results)
    {
        var collection = new Mock<IMongoCollection<TDocument>>();
        collection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<TDocument>>(),
                It.IsAny<FindOptions<TDocument, TDocument>>(),
                It.IsAny<CancellationToken>()))
            .Returns((FilterDefinition<TDocument> filter, FindOptions<TDocument, TDocument> options, CancellationToken _) =>
            {
                calls.Add(new RecordedFind<TDocument>(filter, options));
                return Task.FromResult<IAsyncCursor<TDocument>>(new SingleBatchCursor<TDocument>(results));
            });
        return collection;
    }

    private sealed record Harness<TDocument, TRepository>(
        Mock<IMongoCollection<TDocument>> Collection,
        List<RecordedFind<TDocument>> Calls,
        TRepository Repository);

    private sealed record RecordedFind<TDocument>(
        FilterDefinition<TDocument> Filter,
        FindOptions<TDocument, TDocument> Options);

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
