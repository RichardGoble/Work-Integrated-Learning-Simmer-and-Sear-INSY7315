using Shouldly;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Admin;
using SimmerAndSear.Contracts.Admin;
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
public sealed class NewsServiceTests
{
    private readonly MemoryNewsPostRepository _posts = new();
    private NewsService CreateService() => new(_posts);

    private const string AuthorId = "author-01";
    private const string AuthorName = "Editor";

    [Fact]
    public async Task Created_post_starts_as_draft()
    {
        var service = CreateService();
        var post = await service.CreateAsync(
            new CreateNewsPostRequest { Title = "Hello", Body = "Welcome to our new blog post!", ImageMediaAssetId = "img123" },
            AuthorId, AuthorName);

        post.Status.ShouldBe(PostStatus.Draft.ToString());
        post.PublishedAt.ShouldBeNull();
        post.AuthorId.ShouldBe(AuthorId);
    }

    [Fact]
    public async Task Can_publish_post_with_title_body_and_image()
    {
        var service = CreateService();
        var created = await service.CreateAsync(
            new CreateNewsPostRequest { Title = "Big News", Body = "Important announcement!", ImageMediaAssetId = "img456" },
            AuthorId, AuthorName);

        var (published, error) = await service.PublishAsync(created.Id);

        error.ShouldBeNull();
        published!.Status.ShouldBe(PostStatus.Published.ToString());
        published.PublishedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Cannot_publish_post_without_image()
    {
        var service = CreateService();
        var created = await service.CreateAsync(
            new CreateNewsPostRequest { Title = "Incomplete", Body = "Missing an image for publishing", ImageMediaAssetId = null },
            AuthorId, AuthorName);

        var (published, error) = await service.PublishAsync(created.Id);

        published.ShouldBeNull();
        error.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Cannot_publish_post_without_title()
    {
        var service = CreateService();
        var post = _posts.AddDirectly(new NewsPost
        {
            Id = "direct01",
            Title = "",
            Body = "Body content here",
            ImageMediaAssetId = "img789",
            AuthorId = AuthorId,
            AuthorName = AuthorName,
            Status = PostStatus.Draft
        });

        var (published, error) = await service.PublishAsync(post.Id);

        published.ShouldBeNull();
        error.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Archive_sets_archived_status()
    {
        var service = CreateService();
        var created = await service.CreateAsync(
            new CreateNewsPostRequest { Title = "Old news", Body = "This is old content to archive", ImageMediaAssetId = "img999" },
            AuthorId, AuthorName);

        await service.PublishAsync(created.Id);
        var archived = await service.ArchiveAsync(created.Id);

        archived!.Status.ShouldBe(PostStatus.Archived.ToString());
    }

    [Fact]
    public async Task Update_changes_fields()
    {
        var service = CreateService();
        var created = await service.CreateAsync(
            new CreateNewsPostRequest { Title = "Original", Body = "Original body text", ImageMediaAssetId = "img111" },
            AuthorId, AuthorName);

        var updated = await service.UpdateAsync(created.Id,
            new UpdateNewsPostRequest { Title = "Updated", Body = "Updated body text", ImageMediaAssetId = "img222" });

        updated!.Title.ShouldBe("Updated");
        updated.Body.ShouldBe("Updated body text");
        updated.ImageMediaAssetId.ShouldBe("img222");
    }

    [Fact]
    public async Task Delete_removes_post()
    {
        var service = CreateService();
        var created = await service.CreateAsync(
            new CreateNewsPostRequest { Title = "Delete me", Body = "This will be deleted from store", ImageMediaAssetId = "img333" },
            AuthorId, AuthorName);

        var deleted = await service.DeleteAsync(created.Id);
        deleted.ShouldBeTrue();

        var found = await service.GetAsync(created.Id);
        found.ShouldBeNull();
    }

    [Fact]
    public async Task Returns_null_for_nonexistent_post()
    {
        var service = CreateService();
        var post = await service.GetAsync("000000000000000000000001");
        post.ShouldBeNull();
    }
}

// ────────────────────────────────────────────────────────────────────────────
public sealed class MemoryNewsPostRepository : INewsPostRepository
{
    private readonly List<NewsPost> _items = [];

    public NewsPost AddDirectly(NewsPost p) { _items.Add(p); return p; }

    public Task<NewsPost?> GetAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(_items.Find(p => p.Id == id));

    public Task<Page<NewsPost>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default) =>
        Task.FromResult(new Page<NewsPost>(_items, _items.Count, pageNumber, pageSize));

    public Task<Page<NewsPost>> ListByStatusAsync(PostStatus status, int pageNumber, int pageSize, CancellationToken ct = default) =>
        Task.FromResult(new Page<NewsPost>(_items.Where(p => p.Status == status).ToList(), _items.Count(p => p.Status == status), pageNumber, pageSize));

    public Task<string> CreateAsync(NewsPost post, CancellationToken ct = default)
    {
        post.Id = Guid.NewGuid().ToString("N");
        post.CreatedAt = DateTime.UtcNow;
        post.UpdatedAt = post.CreatedAt;
        _items.Add(post);
        return Task.FromResult(post.Id);
    }

    public Task UpdateAsync(NewsPost post, CancellationToken ct = default)
    {
        var idx = _items.FindIndex(p => p.Id == post.Id);
        if (idx >= 0) _items[idx] = post;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        _items.RemoveAll(p => p.Id == id);
        return Task.CompletedTask;
    }
}
