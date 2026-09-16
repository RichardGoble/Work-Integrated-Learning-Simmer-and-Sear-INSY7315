using MongoDB.Driver;
using SimmerAndSear.Contracts.Paging;

namespace SimmerAndSear.Infrastructure.Persistence;

/// <summary>
/// Shared plumbing for every repository: collection access, the case-insensitive
/// collation, and a paging helper that makes an unbounded query impossible.
/// </summary>
public abstract class MongoRepositoryBase<TEntity> where TEntity : class
{
    /// <summary>
    /// Locale-aware, case- and accent-insensitive comparison.
    /// <para>
    /// A collated index is only used when the query carries the <b>same</b>
    /// collation. Pass it on the find and on the count, or Mongo silently falls
    /// back to a collection scan and the index you built earns nothing.
    /// </para>
    /// </summary>
    protected static readonly Collation CaseInsensitive =
        new("en", strength: CollationStrength.Secondary);

    protected MongoRepositoryBase(IMongoDatabase database, string collectionName)
    {
        Database = database;
        Collection = database.GetCollection<TEntity>(collectionName);
    }

    protected IMongoDatabase Database { get; }

    protected IMongoCollection<TEntity> Collection { get; }

    protected static FilterDefinitionBuilder<TEntity> Filter => Builders<TEntity>.Filter;

    protected static UpdateDefinitionBuilder<TEntity> Update => Builders<TEntity>.Update;

    protected static SortDefinitionBuilder<TEntity> SortBy => Builders<TEntity>.Sort;

    /// <summary>
    /// Runs a bounded, counted query. Every read in the application goes through
    /// here or applies its own explicit Limit - there is no path that returns an
    /// unbounded result set.
    /// </summary>
    protected async Task<Page<TEntity>> PageAsync(
        FilterDefinition<TEntity> filter,
        SortDefinition<TEntity> sort,
        int pageNumber,
        int pageSize,
        bool caseInsensitive = false,
        CancellationToken ct = default)
    {
        var collation = caseInsensitive ? CaseInsensitive : null;

        var total = await Collection.CountDocumentsAsync(
            filter,
            new CountOptions { Collation = collation },
            ct);

        if (total == 0)
        {
            return Page<TEntity>.Empty(pageNumber, pageSize);
        }

        var items = await Collection
            .Find(filter, new FindOptions { Collation = collation })
            .Sort(sort)
            .Skip((pageNumber - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return new Page<TEntity>(items, total, pageNumber, pageSize);
    }
}
