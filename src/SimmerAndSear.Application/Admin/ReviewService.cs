using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Admin;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Application.Admin;

public sealed class ReviewService(IReviewRepository reviews, IProductRepository products)
{
    public async Task<Page<ReviewResponse>> ListAsync(
        int pageNumber, int pageSize, ReviewStatus? status = null, CancellationToken ct = default)
    {
        var page = status.HasValue
            ? await reviews.ListByStatusAsync(status.Value, pageNumber, pageSize, ct)
            : await reviews.ListAsync(pageNumber, pageSize, ct);

        return new Page<ReviewResponse>([.. page.Items.Select(Map)], page.Total, page.PageNumber, page.PageSize);
    }

    public async Task<ReviewResponse?> GetAsync(string id, CancellationToken ct = default)
    {
        var review = await reviews.GetAsync(id, ct);
        return review is null ? null : Map(review);
    }

    public async Task<Page<SimmerAndSear.Contracts.Engagement.PublicReviewResponse>> ListPublicForProductAsync(
        string productId, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var page = await reviews.ListForProductAsync(productId, pageNumber, pageSize, ct);
        var publicItems = page.Items.Select(r => new SimmerAndSear.Contracts.Engagement.PublicReviewResponse(
            r.Id,
            r.ProductId,
            r.ProductName,
            r.ClientName,
            r.Rating,
            r.Title,
            r.Body,
            r.CreatedAt
        )).ToList();

        return new Page<SimmerAndSear.Contracts.Engagement.PublicReviewResponse>(
            publicItems, page.Total, page.PageNumber, page.PageSize);
    }

    public async Task<(ReviewResponse? Review, string? Error)> SubmitAsync(
        string clientId, string clientName, SubmitReviewRequest request, CancellationToken ct = default)
    {
        var product = await products.GetAsync(request.ProductId, ct);
        if (product is null)
            return (null, "Product not found.");

        var eligible = await reviews.HasCompletedOrderForProductAsync(clientId, request.ProductId, ct);
        if (!eligible)
            return (null, "You can only review products from a completed order.");

        var alreadyReviewed = await reviews.HasExistingReviewAsync(clientId, request.ProductId, ct);
        if (alreadyReviewed)
            return (null, "You have already submitted a review for this product.");

        var review = new Review
        {
            ProductId = request.ProductId,
            ProductName = product.Name,
            ClientId = clientId,
            ClientName = clientName,
            Rating = request.Rating,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            Status = ReviewStatus.Pending
        };

        await reviews.CreateAsync(review, ct);
        return (Map(review), null);
    }

    public async Task<(ReviewResponse? Review, string? Error)> EditAsync(
        string id, string clientId, EditReviewRequest request, CancellationToken ct = default)
    {
        var review = await reviews.GetAsync(id, ct);
        if (review is null) return (null, null);
        if (review.ClientId != clientId) return (null, "Forbidden");

        review.UpdateContent(request.Title.Trim(), request.Body.Trim(), request.Rating);
        await reviews.UpdateAsync(review, ct);
        return (Map(review), null);
    }

    public async Task<ReviewResponse?> ApproveAsync(string id, string moderatorId, CancellationToken ct = default)
    {
        var review = await reviews.GetAsync(id, ct);
        if (review is null) return null;

        review.Approve(moderatorId);
        await reviews.UpdateAsync(review, ct);
        return Map(review);
    }

    public async Task<ReviewResponse?> RejectAsync(string id, string moderatorId, CancellationToken ct = default)
    {
        var review = await reviews.GetAsync(id, ct);
        if (review is null) return null;

        review.Reject(moderatorId);
        await reviews.UpdateAsync(review, ct);
        return Map(review);
    }

    private static ReviewResponse Map(Review r) => new(
        r.Id, r.ProductId, r.ProductName, r.ClientId, r.ClientName,
        r.Rating, r.Title, r.Body, r.Status.ToString(),
        r.CreatedAt, r.UpdatedAt, r.ModeratedBy, r.ModeratedAt);
}
