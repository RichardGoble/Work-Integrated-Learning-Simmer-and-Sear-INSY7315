using SimmerAndSear.Contracts.Catalogue;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;
public interface IProductRepository
{
    Task<Product?> GetAsync(string id, CancellationToken ct = default);

    Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default);

    Task<Page<Product>> SearchAsync(ProductFilter filter, CancellationToken ct = default);

    Task<string> CreateAsync(Product product, CancellationToken ct = default);

    Task UpdateAsync(Product product, CancellationToken ct = default);

    Task DeleteAsync(string id, CancellationToken ct = default);
}
