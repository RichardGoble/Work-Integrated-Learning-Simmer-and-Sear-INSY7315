using SimmerAndSear.Contracts.Catalogue;

namespace SimmerAndSear.Application.Abstractions;

public interface IProductAttributeRepository
{
    Task<CatalogueFacets> GetAsync(CancellationToken ct = default);
    Task AddAsync(string field, string value, CancellationToken ct = default);
}
