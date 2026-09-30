using MongoDB.Bson;
using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Catalogue;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.Infrastructure.Repositories;
// ── References ──────────────────────────────────────────────────────────────
//MongoDB, n.d. CRUD Operations [Source code].
//Available at: <https://www.mongodb.com/docs/drivers/csharp/current/crud/> [Accessed 18 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class ProductRepository(IMongoDatabase database)
    : MongoRepositoryBase<Product>(database, MongoCollections.Products), IProductRepository
{
    public async Task<Product?> GetAsync(string id, CancellationToken ct = default) =>
        !IsValidId(id)
            ? null
            : await Collection.Find(Filter.Eq(p => p.Id, id)).Limit(1).FirstOrDefaultAsync(ct);

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
        await Collection
            .Find(Filter.Eq(p => p.Sku, sku), new FindOptions { Collation = CaseInsensitive })
            .Limit(1)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Product>> GetManyAsync(
        IEnumerable<string> ids, CancellationToken ct = default)
    {
        var wanted = ids.Where(IsValidId).Distinct().ToArray();

        if (wanted.Length == 0)
        {
            return [];
        }

        
        return await Collection
            .Find(Filter.In(p => p.Id, wanted))
            .Limit(wanted.Length)
            .ToListAsync(ct);
    }

    public Task<Page<Product>> SearchAsync(ProductFilter filter, CancellationToken ct = default)
    {
        var query = BuildFilter(filter);
        var useCollation = !filter.HasSearchTerm;

        return PageAsync(query, BuildSort(filter), filter.PageNumber, filter.PageSize, useCollation, ct);
    }

    public async Task<CatalogueFacets> GetFacetsAsync(
        bool publishedOnly = true, CancellationToken ct = default)
    {
        var pipeline = new List<BsonDocument>();

        if (publishedOnly)
        {
            pipeline.Add(new BsonDocument("$match", new BsonDocument("isPublished", true)));
        }

        pipeline.Add(new BsonDocument("$group", new BsonDocument
        {
            { "_id", BsonNull.Value },
            { "brands", new BsonDocument("$addToSet", "$brand") },
            { "categories", new BsonDocument("$addToSet", "$category") },
            { "cookwareTypes", new BsonDocument("$addToSet", "$cookwareType") },
            { "materials", new BsonDocument("$addToSet", "$material") },
            { "sizeLabels", new BsonDocument("$addToSet", "$sizeLabel") }
        }));

        var result = await Collection
            .Aggregate<BsonDocument>(pipeline, cancellationToken: ct)
            .FirstOrDefaultAsync(ct);

        return result is null
            ? CatalogueFacets.Empty
            : new CatalogueFacets(
                Values(result, "brands"),
                Values(result, "categories"),
                Values(result, "cookwareTypes"),
                Values(result, "materials"),
                Values(result, "sizeLabels"));
    }

  
    private static List<string> Values(BsonDocument document, string field) =>
        document.TryGetValue(field, out var value) && value is BsonArray array
            ? [.. array.Where(v => !v.IsBsonNull)
                       .Select(v => v.AsString)
                       .Where(v => !string.IsNullOrWhiteSpace(v))
                       .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)]
            : [];

    public async Task<string> CreateAsync(Product product, CancellationToken ct = default)
    {
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = product.CreatedAt;

        try
        {
            await Collection.InsertOneAsync(product, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DuplicateSkuException(product.Sku);
        }

        return product.Id;
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        product.UpdatedAt = DateTime.UtcNow;

        try
        {
            await Collection.ReplaceOneAsync(
                Filter.Eq(p => p.Id, product.Id), product, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DuplicateSkuException(product.Sku);
        }
    }

    public async Task<bool> TryDecrementStockAsync(
        string productId, int quantity, CancellationToken ct = default)
    {
        if (!IsValidId(productId) || quantity <= 0)
        {
            return false;
        }

        var result = await Collection.UpdateOneAsync(
            Filter.And(
                Filter.Eq(p => p.Id, productId),
                Filter.Gte(p => p.StockQuantity, quantity)),
            Update.Inc(p => p.StockQuantity, -quantity)
                  .Set(p => p.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        return result.ModifiedCount == 1;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        if (IsValidId(id))
        {
            await Collection.DeleteOneAsync(Filter.Eq(p => p.Id, id), ct);
        }
    }

    private static FilterDefinition<Product> BuildFilter(ProductFilter f)
    {
        var clauses = new List<FilterDefinition<Product>>();

        if (f.PublishedOnly)
        {
            clauses.Add(Filter.Eq(p => p.IsPublished, true));
        }

        if (f.HasSearchTerm)
        {
            clauses.Add(Filter.Text(f.SearchTerm!));
        }

        if (!string.IsNullOrWhiteSpace(f.Category))
        {
            clauses.Add(Filter.Eq(p => p.Category, f.Category));
        }

        if (!string.IsNullOrWhiteSpace(f.Brand))
        {
            clauses.Add(Filter.Eq(p => p.Brand, f.Brand));
        }

        if (!string.IsNullOrWhiteSpace(f.CookwareType))
        {
            clauses.Add(Filter.Eq(p => p.CookwareType, f.CookwareType));
        }

        if (!string.IsNullOrWhiteSpace(f.Material))
        {
            clauses.Add(Filter.Eq(p => p.Material, f.Material));
        }

        if (!string.IsNullOrWhiteSpace(f.SizeLabel))
        {
            clauses.Add(Filter.Eq(p => p.SizeLabel, f.SizeLabel));
        }

        if (f.MinPrice is { } min)
        {
            clauses.Add(Filter.Gte(p => p.Price, min));
        }

        if (f.MaxPrice is { } max)
        {
            clauses.Add(Filter.Lte(p => p.Price, max));
        }

        return clauses.Count == 0 ? Filter.Empty : Filter.And(clauses);
    }

    private static SortDefinition<Product> BuildSort(ProductFilter f) => f.Sort switch
    {
        ProductSort.NameAscending => SortBy.Ascending(p => p.Name),
        ProductSort.NameDescending => SortBy.Descending(p => p.Name),
        ProductSort.PriceAscending => SortBy.Ascending(p => p.Price),
        ProductSort.PriceDescending => SortBy.Descending(p => p.Price),
        ProductSort.Newest => SortBy.Descending(p => p.CreatedAt),
        _ => SortBy.Descending(p => p.CreatedAt)
    };
}
