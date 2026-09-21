using MongoDB.Driver;
using SimmerAndSear.Contracts.Paging;

namespace SimmerAndSear.Infrastructure.Persistence;
// ── References ──────────────────────────────────────────────────────────────
//MongoDB, n.d. CRUD Operations [Source code].
//Available at: <https://www.mongodb.com/docs/drivers/csharp/current/crud/> [Accessed 18 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
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

    protected static bool IsValidId(string? id) =>
        id is { Length: 24 } && id.All(char.IsAsciiHexDigit);

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
