using SimmerAndSear.Contracts.Catalogue;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 18 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public interface IProductRepository
{
    Task<Product?> GetAsync(string id, CancellationToken ct = default);

    Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default);

    Task<IReadOnlyList<Product>> GetManyAsync(IEnumerable<string> ids, CancellationToken ct = default);

    Task<Page<Product>> SearchAsync(ProductFilter filter, CancellationToken ct = default);

    Task<string> CreateAsync(Product product, CancellationToken ct = default);

    Task UpdateAsync(Product product, CancellationToken ct = default);

    Task DeleteAsync(string id, CancellationToken ct = default);
}
