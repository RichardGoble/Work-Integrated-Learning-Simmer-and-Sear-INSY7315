using SimmerAndSear.Contracts.Catalogue;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;

/// <summary>
/// Repository - the catalogue aggregate's only route to storage. Defined here in
/// the application layer so that services and their unit tests depend on this
/// contract rather than on MongoDB.
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetAsync(string id, CancellationToken ct = default);

    Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default);

    Task<Page<Product>> SearchAsync(ProductFilter filter, CancellationToken ct = default);

    Task<string> CreateAsync(Product product, CancellationToken ct = default);

    Task UpdateAsync(Product product, CancellationToken ct = default);

    Task DeleteAsync(string id, CancellationToken ct = default);
}
