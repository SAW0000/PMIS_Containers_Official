using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace PMIS.Infrastructure.Mongo;

internal sealed class NewestFirstPager<TDocument, TSortKey> where TDocument : class
{
    private readonly IMongoCollection<TDocument> _collection;
    private readonly Expression<Func<TDocument, TSortKey>> _sortKey;
    private readonly Expression<Func<TDocument, ObjectId>> _documentId;
    private readonly Func<TDocument, TSortKey> _readSortKey;
    private readonly Func<TDocument, ObjectId> _readDocumentId;
    private readonly SortDefinition<TDocument> _sort;
    private readonly ProjectionDefinition<TDocument> _boundaryProjection;

    internal NewestFirstPager(
        IMongoCollection<TDocument> collection,
        Expression<Func<TDocument, TSortKey>> sortKey,
        Expression<Func<TDocument, ObjectId>> documentId)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(sortKey);
        ArgumentNullException.ThrowIfNull(documentId);

        _collection = collection;
        _sortKey = sortKey;
        _documentId = documentId;
        _readSortKey = sortKey.Compile();
        _readDocumentId = documentId.Compile();

        var sortKeyName = ElementName(sortKey);
        var documentIdName = ElementName(documentId);
        _sort = new BsonDocument { { sortKeyName, -1 }, { documentIdName, -1 } };
        _boundaryProjection = new BsonDocument { { sortKeyName, 1 }, { documentIdName, 1 } };
    }

    private static string ElementName<TField>(Expression<Func<TDocument, TField>> field)
    {
        if (field.Body is not MemberExpression member)
        {
            throw new ArgumentException("Key must be a direct property access.", nameof(field));
        }

        var memberMap = BsonClassMap.LookupClassMap(typeof(TDocument)).GetMemberMap(member.Member.Name)
            ?? throw new ArgumentException($"'{member.Member.Name}' is not a mapped field.", nameof(field));

        return memberMap.ElementName;
    }

    internal async Task<IReadOnlyList<TDocument>> GetPageAsync(
        FilterDefinition<TDocument> filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var skipCount = SkipCount(page, pageSize);
        if (skipCount == 0)
        {
            return await ReadPageAsync(filter, pageSize, cancellationToken);
        }

        var boundary = await FindBoundaryAsync(filter, skipCount, cancellationToken);
        if (boundary is null)
        {
            return [];
        }

        var afterBoundary = filter & BuildAfterBoundaryFilter(boundary);
        return await ReadPageAsync(afterBoundary, pageSize, cancellationToken);
    }

    private static int SkipCount(int page, int pageSize)
    {
        if (page <= 1 || pageSize <= 0)
        {
            return 0;
        }

        var skipCount = (long)(page - 1) * pageSize;
        return skipCount >= int.MaxValue ? int.MaxValue : (int)skipCount;
    }

    private async Task<IReadOnlyList<TDocument>> ReadPageAsync(
        FilterDefinition<TDocument> filter, int pageSize, CancellationToken cancellationToken)
        => await _collection
            .Find(filter)
            .Sort(_sort)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

    private Task<TDocument?> FindBoundaryAsync(
        FilterDefinition<TDocument> filter, int skipCount, CancellationToken cancellationToken)
        => _collection
            .Find(filter)
            .Sort(_sort)
            .Skip(skipCount - 1)
            .Limit(1)
            .Project<TDocument>(_boundaryProjection)
            .FirstOrDefaultAsync(cancellationToken)!;

    private FilterDefinition<TDocument> BuildAfterBoundaryFilter(TDocument boundary)
    {
        var filter = Builders<TDocument>.Filter;
        var boundarySortKey = _readSortKey(boundary);
        var boundaryId = _readDocumentId(boundary);

        return filter.Lte(_sortKey, boundarySortKey)
             & (filter.Lt(_sortKey, boundarySortKey) | filter.Lt(_documentId, boundaryId));
    }
}
