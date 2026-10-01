using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Application.Abstractions;

public interface INewsPostRepository
{
    Task<NewsPost?> GetAsync(string id, CancellationToken ct = default);

    Task<Page<NewsPost>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default);

    Task<Page<NewsPost>> ListByStatusAsync(PostStatus status, int pageNumber, int pageSize, CancellationToken ct = default);

    Task<string> CreateAsync(NewsPost post, CancellationToken ct = default);

    Task UpdateAsync(NewsPost post, CancellationToken ct = default);

    Task DeleteAsync(string id, CancellationToken ct = default);
}
