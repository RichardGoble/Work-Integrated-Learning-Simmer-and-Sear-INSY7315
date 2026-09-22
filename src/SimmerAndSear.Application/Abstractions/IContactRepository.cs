using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;

public interface IContactRepository
{
    Task<List<ContactRequest>> ListAsync(CancellationToken ct = default);
    Task<string> CreateAsync(ContactRequest request, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
