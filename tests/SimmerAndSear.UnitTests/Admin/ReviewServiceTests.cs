using Shouldly;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Admin;
using SimmerAndSear.Contracts.Admin;
using SimmerAndSear.Contracts.Catalogue;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.UnitTests.Admin;

// ── References ──────────────────────────────────────────────────────────────
//xUnit.net, 2025. Getting Started with xUnit.net v2 [Source code].
//Available at: <https://xunit.net/docs/getting-started/v2/getting-started> [Accessed 21 September 2026].
//
//Shouldly, n.d. Shouldly Documentation [Source code].
//Available at: <https://docs.shouldly.org/> [Accessed 21 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class ReviewServiceTests
{
    private readonly MemoryReviewRepository _reviews = new();
    private readonly MemoryProductRepository _products = new();

    private ReviewService CreateService() => new(_reviews, _products);

    private const string ClientId = "client-01";
    private const string ClientName = "Alice";
    private const string ProductId = "prod-abc123456789012";

    private Product AddProduct(string id = ProductId) =>
        _products.Add(new Product { Id = id, Name = "Test Product", IsPublished = true });

    private void AddCompletedOrder(string productId = ProductId) =>
        _reviews.AddCompletedOrder(ClientId, productId);

    [Fact]
    public async Task Review_requires_completed_order_for_product()
    {
        AddProduct();
        var service = CreateService();

        var (review, error) = await service.SubmitAsync(ClientId, ClientName,
            new SubmitReviewRequest { ProductId = ProductId, Rating = 5, Title = "Great", Body = "Really loved it" });

        review.ShouldBeNull();
        error.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Can_submit_review_with_completed_order()
    {
        AddProduct();
        AddCompletedOrder();
        var service = CreateService();

        var (review, error) = await service.SubmitAsync(ClientId, ClientName,
            new SubmitReviewRequest { ProductId = ProductId, Rating = 4, Title = "Good", Body = "Would buy again" });

        error.ShouldBeNull();
        review.ShouldNotBeNull();
        review!.Status.ShouldBe(ReviewStatus.Pending.ToString());
        review.Rating.ShouldBe(4);
    }

    [Fact]
    public async Task Cannot_submit_duplicate_review()
    {
        AddProduct();
        AddCompletedOrder();
        var service = CreateService();

        await service.SubmitAsync(ClientId, ClientName,
            new SubmitReviewRequest { ProductId = ProductId, Rating = 5, Title = "Great", Body = "Really loved it" });

        var (second, error) = await service.SubmitAsync(ClientId, ClientName,
            new SubmitReviewRequest { ProductId = ProductId, Rating = 3, Title = "OK", Body = "Changed my mind" });

        second.ShouldBeNull();
        error.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Approve_sets_status_and_moderator()
    {
        AddProduct();
        AddCompletedOrder();
        var service = CreateService();

        var (submitted, _) = await service.SubmitAsync(ClientId, ClientName,
            new SubmitReviewRequest { ProductId = ProductId, Rating = 5, Title = "Excellent", Body = "Perfect product" });

        var approved = await service.ApproveAsync(submitted!.Id, "mod-01");

        approved!.Status.ShouldBe(ReviewStatus.Approved.ToString());
        approved.ModeratedBy.ShouldBe("mod-01");
        approved.ModeratedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Reject_sets_rejected_status()
    {
        AddProduct();
        AddCompletedOrder();
        var service = CreateService();

        var (submitted, _) = await service.SubmitAsync(ClientId, ClientName,
            new SubmitReviewRequest { ProductId = ProductId, Rating = 1, Title = "Bad", Body = "Would not recommend at all" });

        var rejected = await service.RejectAsync(submitted!.Id, "mod-01");

        rejected!.Status.ShouldBe(ReviewStatus.Rejected.ToString());
    }

    [Fact]
    public async Task Editing_approved_review_returns_it_to_pending()
    {
        AddProduct();
        AddCompletedOrder();
        var service = CreateService();

        var (submitted, _) = await service.SubmitAsync(ClientId, ClientName,
            new SubmitReviewRequest { ProductId = ProductId, Rating = 5, Title = "Love it", Body = "Outstanding quality product" });

        await service.ApproveAsync(submitted!.Id, "mod-01");

        var (edited, error) = await service.EditAsync(submitted.Id, ClientId,
            new EditReviewRequest { Rating = 4, Title = "Still good", Body = "Minor concern but still great" });

        error.ShouldBeNull();
        edited!.Status.ShouldBe(ReviewStatus.Pending.ToString());
    }

    [Fact]
    public async Task Customer_cannot_edit_another_customers_review()
    {
        AddProduct();
        AddCompletedOrder();
        var service = CreateService();

        var (submitted, _) = await service.SubmitAsync(ClientId, ClientName,
            new SubmitReviewRequest { ProductId = ProductId, Rating = 3, Title = "Fine", Body = "Nothing special to report here" });

        var (result, error) = await service.EditAsync(submitted!.Id, "other-client",
            new EditReviewRequest { Rating = 1, Title = "Spam", Body = "Trying to hijack review content" });

        error.ShouldBe("Forbidden");
        result.ShouldBeNull();
    }

    [Fact]
    public async Task Returns_null_for_nonexistent_review()
    {
        var service = CreateService();
        var review = await service.GetAsync("000000000000000000000001");
        review.ShouldBeNull();
    }

    [Fact]
    public async Task Returns_null_for_nonexistent_product()
    {
        var service = CreateService();

        var (review, error) = await service.SubmitAsync(ClientId, ClientName,
            new SubmitReviewRequest { ProductId = "nonexistent-product", Rating = 5, Title = "Test", Body = "Testing nonexistent product" });

        review.ShouldBeNull();
        error.ShouldNotBeNullOrEmpty();
    }
}

// ────────────────────────────────────────────────────────────────────────────
public sealed class MemoryReviewRepository : IReviewRepository
{
    private readonly List<Review> _items = [];
    private readonly List<(string ClientId, string ProductId)> _completedOrders = [];

    public void AddCompletedOrder(string clientId, string productId) =>
        _completedOrders.Add((clientId, productId));

    public Task<Review?> GetAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(_items.Find(r => r.Id == id));

    public Task<Page<Review>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default) =>
        Task.FromResult(new Page<Review>(_items, _items.Count, pageNumber, pageSize));

    public Task<Page<Review>> ListByStatusAsync(ReviewStatus status, int pageNumber, int pageSize, CancellationToken ct = default) =>
        Task.FromResult(new Page<Review>(_items.Where(r => r.Status == status).ToList(), _items.Count(r => r.Status == status), pageNumber, pageSize));

    public Task<Page<Review>> ListForProductAsync(string productId, int pageNumber, int pageSize, CancellationToken ct = default) =>
        Task.FromResult(new Page<Review>(_items.Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved).ToList(), 0, pageNumber, pageSize));

    public Task<bool> HasCompletedOrderForProductAsync(string clientId, string productId, CancellationToken ct = default) =>
        Task.FromResult(_completedOrders.Any(o => o.ClientId == clientId && o.ProductId == productId));

    public Task<bool> HasExistingReviewAsync(string clientId, string productId, CancellationToken ct = default) =>
        Task.FromResult(_items.Any(r => r.ClientId == clientId && r.ProductId == productId));

    public Task<string> CreateAsync(Review review, CancellationToken ct = default)
    {
        review.Id = Guid.NewGuid().ToString("N");
        review.CreatedAt = DateTime.UtcNow;
        review.UpdatedAt = review.CreatedAt;
        _items.Add(review);
        return Task.FromResult(review.Id);
    }

    public Task UpdateAsync(Review review, CancellationToken ct = default)
    {
        var idx = _items.FindIndex(r => r.Id == review.Id);
        if (idx >= 0) _items[idx] = review;
        return Task.CompletedTask;
    }
}

// ────────────────────────────────────────────────────────────────────────────
public sealed class MemoryProductRepository : IProductRepository
{
    private readonly List<Product> _items = [];

    public Product Add(Product p) { _items.Add(p); return p; }

    public Task<Product?> GetAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(_items.Find(p => p.Id == id));

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
        Task.FromResult(_items.Find(p => p.Sku == sku));

    public Task<Page<Product>> SearchAsync(SimmerAndSear.Contracts.Catalogue.ProductFilter filter, CancellationToken ct = default) =>
        Task.FromResult(new Page<Product>(_items, _items.Count, filter.PageNumber, filter.PageSize));

    public Task<CatalogueFacets> GetFacetsAsync(bool publishedOnly = true, CancellationToken ct = default) =>
        Task.FromResult(CatalogueFacets.Empty);

    public Task<IReadOnlyList<Product>> GetManyAsync(IEnumerable<string> ids, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Product>>(_items.Where(p => ids.Contains(p.Id)).ToList());

    public Task<string> CreateAsync(Product product, CancellationToken ct = default)
    {
        product.Id ??= Guid.NewGuid().ToString("N");
        _items.Add(product);
        return Task.FromResult(product.Id);
    }

    public Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        var idx = _items.FindIndex(p => p.Id == product.Id);
        if (idx >= 0) _items[idx] = product;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var p = _items.Find(x => x.Id == id);
        if (p is not null) _items.Remove(p);
        return Task.CompletedTask;
    }
}
