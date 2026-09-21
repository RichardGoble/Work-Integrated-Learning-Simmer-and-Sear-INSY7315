using SimmerAndSear.Domain.Abstractions;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Domain.Entities;

public sealed class Review : IEntity
{
    public string Id { get; set; } = string.Empty;

    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;

    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? ModeratedBy { get; set; }
    public DateTime? ModeratedAt { get; set; }

    public void Approve(string moderatorId)
    {
        Status = ReviewStatus.Approved;
        ModeratedBy = moderatorId;
        ModeratedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(string moderatorId)
    {
        Status = ReviewStatus.Rejected;
        ModeratedBy = moderatorId;
        ModeratedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateContent(string title, string body, int rating)
    {
        Title = title;
        Body = body;
        Rating = rating;
        UpdatedAt = DateTime.UtcNow;

        if (Status == ReviewStatus.Approved)
        {
            Status = ReviewStatus.Pending;
            ModeratedBy = null;
            ModeratedAt = null;
        }
    }
}
