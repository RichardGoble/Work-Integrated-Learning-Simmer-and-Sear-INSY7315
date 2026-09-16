using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Catalogue;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.Infrastructure.Repositories;

/// <summary>
/// Repository for the catalogue aggregate. This is the reference implementation:
/// every other repository in the solution follows the same shape - translate the
/// filter, choose the sort, page through the base class, never return unbounded
/// results, never leak a driver type past this assembly.
/// </summary>
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

    public Task<Page<Product>> SearchAsync(ProductFilter filter, CancellationToken ct = default)
    {
        var query = BuildFilter(filter);

        // A $text query cannot carry a collation, so the two index strategies are
        // mutually exclusive: free-text search uses the text index, everything
        // else uses the collated compound index.
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

        // Relevance currently falls back to newest-first. Ranking by $meta
        // textScore is the catalogue slice's job - it needs the score projected,
        // and getting it wrong is a runtime error rather than a compile error.
        _ => SortBy.Descending(p => p.CreatedAt)
    };
}
