using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;

public interface ICartRepository
{
    Task<Cart?> GetByClientAsync(string clientId, CancellationToken ct = default);

    Task<Cart?> GetBySessionAsync(string sessionId, CancellationToken ct = default);

    Task SaveAsync(Cart cart, CancellationToken ct = default);

    Task DeleteAsync(string cartId, CancellationToken ct = default);
}
