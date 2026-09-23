using SimmerAndSear.Domain.Abstractions;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Domain.Entities;

public sealed class ContactRequest : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string InquiryType { get; set; } = "General Inquiry";
    public string Message { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ContactStatus Status { get; set; } = ContactStatus.New;
    public string? HandledBy { get; set; }
    public ContactNotificationStatus NotificationStatus { get; set; } = ContactNotificationStatus.Pending;
    public DateTime? NotificationSentAt { get; set; }
    public string? NotificationError { get; set; }
}
