using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;

public interface IOrderRepository
{
    Task<Order?> GetAsync(string id, CancellationToken ct = default);

    
    Task<Order?> GetForClientAsync(string id, string clientId, CancellationToken ct = default);

    Task<Page<Order>> ListForClientAsync(
        string clientId, int pageNumber, int pageSize, CancellationToken ct = default);

    Task<Page<Order>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default);

    Task<string> CreateAsync(Order order, CancellationToken ct = default);

    Task UpdateAsync(Order order, CancellationToken ct = default);
}
