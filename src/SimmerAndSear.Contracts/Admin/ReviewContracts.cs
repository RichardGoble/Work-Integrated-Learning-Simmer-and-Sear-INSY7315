using System.ComponentModel.DataAnnotations;

namespace SimmerAndSear.Contracts.Admin;

public sealed class SubmitReviewRequest
{
    [Required]
    public string ProductId { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; }

    [Required, StringLength(160, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 10)]
    public string Body { get; set; } = string.Empty;
}

public sealed class EditReviewRequest
{
    [Range(1, 5)]
    public int Rating { get; set; }

    [Required, StringLength(160, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 10)]
    public string Body { get; set; } = string.Empty;
}

public sealed record ReviewResponse(
    string Id,
    string ProductId,
    string ProductName,
    string ClientId,
    string ClientName,
    int Rating,
    string Title,
    string Body,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? ModeratedBy,
    DateTime? ModeratedAt);
