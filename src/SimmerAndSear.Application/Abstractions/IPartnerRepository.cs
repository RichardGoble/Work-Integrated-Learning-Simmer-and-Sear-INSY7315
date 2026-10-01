using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;

public interface IPartnerRepository
{
    Task<List<RetailPartner>> ListAsync(CancellationToken ct = default);
    Task<string> CreateAsync(RetailPartner partner, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
