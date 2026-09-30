using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Admin;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Application.Admin;

public sealed class NewsService(INewsPostRepository newsPosts)
{
    public async Task<Page<NewsPostResponse>> ListAsync(
        int pageNumber, int pageSize, PostStatus? status = null, CancellationToken ct = default)
    {
        var page = status.HasValue
            ? await newsPosts.ListByStatusAsync(status.Value, pageNumber, pageSize, ct)
            : await newsPosts.ListAsync(pageNumber, pageSize, ct);

        return new Page<NewsPostResponse>([.. page.Items.Select(Map)], page.Total, page.PageNumber, page.PageSize);
    }


    public async Task<Page<NewsPostResponse>> ListPublishedAsync(
        int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var page = await newsPosts.ListByStatusAsync(PostStatus.Published, pageNumber, pageSize, ct);
        return new Page<NewsPostResponse>([.. page.Items.Select(Map)], page.Total, page.PageNumber, page.PageSize);
    }


    public async Task<NewsPostResponse?> GetPublishedAsync(string id, CancellationToken ct = default)
    {
        var post = await newsPosts.GetAsync(id, ct);

        return post is null || post.Status != PostStatus.Published ? null : Map(post);
    }

    public async Task<NewsPostResponse?> GetAsync(string id, CancellationToken ct = default)
    {
        var post = await newsPosts.GetAsync(id, ct);
        return post is null ? null : Map(post);
    }

    public async Task<NewsPostResponse> CreateAsync(
        CreateNewsPostRequest request, string authorId, string authorName, CancellationToken ct = default)
    {
        var post = new NewsPost
        {
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            ImageMediaAssetId = Clean(request.ImageMediaAssetId),
            AuthorId = authorId,
            AuthorName = authorName,
            Status = PostStatus.Draft
        };

        await newsPosts.CreateAsync(post, ct);
        return Map(post);
    }

    public async Task<NewsPostResponse?> UpdateAsync(
        string id, UpdateNewsPostRequest request, CancellationToken ct = default)
    {
        var post = await newsPosts.GetAsync(id, ct);
        if (post is null) return null;

        post.Title = request.Title.Trim();
        post.Body = request.Body.Trim();
        post.ImageMediaAssetId = Clean(request.ImageMediaAssetId);

        await newsPosts.UpdateAsync(post, ct);
        return Map(post);
    }

    public async Task<(NewsPostResponse? Post, string? Error)> PublishAsync(string id, CancellationToken ct = default)
    {
        var post = await newsPosts.GetAsync(id, ct);
        if (post is null) return (null, null);

        try
        {
            post.Publish();
        }
        catch (InvalidOperationException ex)
        {
            return (null, ex.Message);
        }

        await newsPosts.UpdateAsync(post, ct);
        return (Map(post), null);
    }

    public async Task<NewsPostResponse?> ArchiveAsync(string id, CancellationToken ct = default)
    {
        var post = await newsPosts.GetAsync(id, ct);
        if (post is null) return null;

        post.Archive();
        await newsPosts.UpdateAsync(post, ct);
        return Map(post);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var post = await newsPosts.GetAsync(id, ct);
        if (post is null) return false;

        await newsPosts.DeleteAsync(id, ct);
        return true;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static NewsPostResponse Map(NewsPost p) => new(
        p.Id, p.Title, p.Body, p.ImageMediaAssetId,
        p.Status.ToString(), p.AuthorId, p.AuthorName, p.CreatedAt, p.UpdatedAt, p.PublishedAt);
}
