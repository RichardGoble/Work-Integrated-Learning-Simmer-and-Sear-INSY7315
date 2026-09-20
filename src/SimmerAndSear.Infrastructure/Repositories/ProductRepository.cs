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
        await Collection.Find(Filter.Eq(p => p.Id, id)).Limit(1).FirstOrDefaultAsync(ct);

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
        await Collection
            .Find(Filter.Eq(p => p.Sku, sku), new FindOptions { Collation = CaseInsensitive })
            .Limit(1)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Product>> GetManyAsync(
        IEnumerable<string> ids, CancellationToken ct = default)
    {
        var wanted = ids.Distinct().ToArray();

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

    public async Task<string> CreateAsync(Product product, CancellationToken ct = default)
    {
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = product.CreatedAt;

        await Collection.InsertOneAsync(product, cancellationToken: ct);
        return product.Id;
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        product.UpdatedAt = DateTime.UtcNow;
        await Collection.ReplaceOneAsync(Filter.Eq(p => p.Id, product.Id), product, cancellationToken: ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default) =>
        await Collection.DeleteOneAsync(Filter.Eq(p => p.Id, id), ct);

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
