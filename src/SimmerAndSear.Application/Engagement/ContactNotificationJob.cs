namespace SimmerAndSear.Application.Engagement;

public sealed record ContactNotificationJob(
    string ContactRequestId,
    string FullName,
    string Email,
    string? Phone,
    string InquiryType,
    string Message,
    DateTime SubmittedAt
);
