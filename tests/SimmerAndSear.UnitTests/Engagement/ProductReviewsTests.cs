using Shouldly;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Admin;
using SimmerAndSear.Contracts.Admin;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.UnitTests.Engagement;

public sealed class ProductReviewsTests
{
    [Fact]
    public async Task ListPublicForProductAsync_returns_approved_only_and_hides_client_and_moderation_secrets()
    {
        var reviewRepo = new MemoryReviewRepo();
        var productRepo = new MemoryProductRepo();
        var service = new ReviewService(reviewRepo, productRepo);

        reviewRepo.Items.Add(new Review
        {
            Id = "rev_approved",
            ProductId = "prod_cast_iron",
            ProductName = "Cast Iron Skillet",
            ClientId = "secret_client_id_1",
            ClientName = "Chef Pierre",
            Rating = 5,
            Title = "Masterpiece pan",
            Body = "Exceptional heat retention.",
            Status = ReviewStatus.Approved,
            ModeratedBy = "moderator_admin_9",
            ModeratedAt = DateTime.UtcNow
        });

        reviewRepo.Items.Add(new Review
        {
            Id = "rev_pending",
            ProductId = "prod_cast_iron",
            ProductName = "Cast Iron Skillet",
            ClientId = "secret_client_id_2",
            ClientName = "Suspicious User",
            Rating = 1,
            Title = "Pending review",
            Body = "Should not be public yet.",
            Status = ReviewStatus.Pending
        });

        reviewRepo.Items.Add(new Review
        {
            Id = "rev_rejected",
            ProductId = "prod_cast_iron",
            ProductName = "Cast Iron Skillet",
            ClientId = "secret_client_id_3",
            ClientName = "Spam User",
            Rating = 1,
            Title = "Rejected spam",
            Body = "Spam content.",
            Status = ReviewStatus.Rejected
        });

        var publicPage = await service.ListPublicForProductAsync("prod_cast_iron", 1, 20);

        publicPage.Items.Count.ShouldBe(1);
        var review = publicPage.Items[0];
        review.Id.ShouldBe("rev_approved");
        review.ClientName.ShouldBe("Chef Pierre");
        review.Title.ShouldBe("Masterpiece pan");
        review.Rating.ShouldBe(5);

        // Verify the public DTO type has no ClientId or ModeratedBy fields
        typeof(SimmerAndSear.Contracts.Engagement.PublicReviewResponse).GetProperty("ClientId").ShouldBeNull();
        typeof(SimmerAndSear.Contracts.Engagement.PublicReviewResponse).GetProperty("ModeratedBy").ShouldBeNull();
    }

    [Fact]
    public async Task SubmitAsync_enforces_review_eligibility_from_delivered_order()
    {
        var reviewRepo = new MemoryReviewRepo();
        var productRepo = new MemoryProductRepo();
        productRepo.Items.Add(new Product { Id = "prod_wok", Name = "Carbon Steel Wok" });

        var service = new ReviewService(reviewRepo, productRepo);

        // Client has NOT ordered the product
        var (review, error) = await service.SubmitAsync("cust_no_order", "Customer", new SubmitReviewRequest
        {
            ProductId = "prod_wok",
            Rating = 5,
            Title = "Great wok",
            Body = "Looks fantastic but never bought it."
        });

        review.ShouldBeNull();
        error.ShouldNotBeNull();
        error.ShouldContain("completed order");
    }

    private sealed class MemoryReviewRepo : IReviewRepository
    {
        public List<Review> Items { get; } = [];

        public Task<Review?> GetAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(r => r.Id == id));

        public Task<Page<Review>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default) =>
            Task.FromResult(new Page<Review>(Items, Items.Count, pageNumber, pageSize));

        public Task<Page<Review>> ListByStatusAsync(ReviewStatus status, int pageNumber, int pageSize, CancellationToken ct = default) =>
            Task.FromResult(new Page<Review>(Items.Where(r => r.Status == status).ToList(), Items.Count, pageNumber, pageSize));

        public Task<Page<Review>> ListForProductAsync(string productId, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var approved = Items.Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved).ToList();
            return Task.FromResult(new Page<Review>(approved, approved.Count, pageNumber, pageSize));
        }

        public Task<bool> HasCompletedOrderForProductAsync(string clientId, string productId, CancellationToken ct = default) =>
            Task.FromResult(clientId == "cust_with_order");

        public Task<bool> HasExistingReviewAsync(string clientId, string productId, CancellationToken ct = default) =>
            Task.FromResult(Items.Any(r => r.ClientId == clientId && r.ProductId == productId));

        public Task<string> CreateAsync(Review review, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(review.Id)) review.Id = Guid.NewGuid().ToString("N");
            Items.Add(review);
            return Task.FromResult(review.Id);
        }

        public Task UpdateAsync(Review review, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(string id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class MemoryProductRepo : IProductRepository
    {
        public List<Product> Items { get; } = [];

        public Task<Product?> GetAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(p => p.Id == id));

        public Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(p => p.Sku == sku));

        public Task<IReadOnlyList<Product>> GetManyAsync(IEnumerable<string> ids, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Product>>(Items.Where(p => ids.Contains(p.Id)).ToList());

        public Task<Page<Product>> SearchAsync(SimmerAndSear.Contracts.Catalogue.ProductFilter filter, CancellationToken ct = default) =>
            Task.FromResult(new Page<Product>(Items, Items.Count, 1, 20));

        public Task<SimmerAndSear.Contracts.Catalogue.CatalogueFacets> GetFacetsAsync(bool publishedOnly = true, CancellationToken ct = default) =>
            Task.FromResult(SimmerAndSear.Contracts.Catalogue.CatalogueFacets.Empty);

        public Task<string> CreateAsync(Product product, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(Product product, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> TryDecrementStockAsync(string productId, int quantity, CancellationToken ct = default)
        {
            var p = Items.Find(x => x.Id == productId);
            if (p is null || quantity <= 0 || p.StockQuantity < quantity) return Task.FromResult(false);
            p.StockQuantity -= quantity;
            return Task.FromResult(true);
        }

        public Task DeleteAsync(string id, CancellationToken ct = default) => throw new NotImplementedException();
    }
}
