using MongoDB.Driver;
using SimmerAndSear.Contracts.Paging;

namespace SimmerAndSear.Infrastructure.Persistence;
public abstract class MongoRepositoryBase<TEntity> where TEntity : class
{
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
