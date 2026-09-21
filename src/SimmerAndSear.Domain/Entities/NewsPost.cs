using SimmerAndSear.Domain.Abstractions;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Domain.Entities;

public sealed class NewsPost : IEntity
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ImageMediaAssetId { get; set; }

    public PostStatus Status { get; set; } = PostStatus.Draft;

    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }

    public bool CanPublish =>
        !string.IsNullOrWhiteSpace(Title) &&
        !string.IsNullOrWhiteSpace(Body) &&
        !string.IsNullOrWhiteSpace(ImageMediaAssetId);

    public void Publish()
    {
        if (!CanPublish)
            throw new InvalidOperationException("A news post requires a title, body and image before it can be published.");

        Status = PostStatus.Published;
        PublishedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = PostStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }
}
