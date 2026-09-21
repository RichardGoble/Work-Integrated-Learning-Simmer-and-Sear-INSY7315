using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Application.Abstractions;

public interface IReviewRepository
{
    Task<Review?> GetAsync(string id, CancellationToken ct = default);

    Task<Page<Review>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default);

    Task<Page<Review>> ListByStatusAsync(ReviewStatus status, int pageNumber, int pageSize, CancellationToken ct = default);

    Task<Page<Review>> ListForProductAsync(string productId, int pageNumber, int pageSize, CancellationToken ct = default);

    Task<bool> HasCompletedOrderForProductAsync(string clientId, string productId, CancellationToken ct = default);

    Task<bool> HasExistingReviewAsync(string clientId, string productId, CancellationToken ct = default);

    Task<string> CreateAsync(Review review, CancellationToken ct = default);

    Task UpdateAsync(Review review, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
