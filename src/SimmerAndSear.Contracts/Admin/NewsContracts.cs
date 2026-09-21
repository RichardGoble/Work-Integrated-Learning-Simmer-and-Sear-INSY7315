using System.ComponentModel.DataAnnotations;

namespace SimmerAndSear.Contracts.Admin;

public sealed class CreateNewsPostRequest
{
    [Required, StringLength(160, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(50000, MinimumLength = 10)]
    public string Body { get; set; } = string.Empty;

    public string? ImageMediaAssetId { get; set; }
}

public sealed class UpdateNewsPostRequest
{
    [Required, StringLength(160, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(50000, MinimumLength = 10)]
    public string Body { get; set; } = string.Empty;

    public string? ImageMediaAssetId { get; set; }
}

public sealed record NewsPostResponse(
    string Id,
    string Title,
    string Body,
    string? ImageMediaAssetId,
    string Status,
    string AuthorId,
    string AuthorName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? PublishedAt)
{
    public string? ImageUrl => string.IsNullOrWhiteSpace(ImageMediaAssetId)
        ? null
        : $"/api/media/{ImageMediaAssetId}";
}
