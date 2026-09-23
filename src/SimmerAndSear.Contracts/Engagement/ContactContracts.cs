using System.ComponentModel.DataAnnotations;

namespace SimmerAndSear.Contracts.Engagement;

public sealed record CreateContactRequest(
    [Required, StringLength(120, MinimumLength = 1)]
    string FullName,

    [Required, EmailAddress, StringLength(200)]
    string Email,

    [StringLength(30)]
    string? Phone,

    [Required, StringLength(100, MinimumLength = 1)]
    string InquiryType,

    [Required, StringLength(2000, MinimumLength = 2)]
    string Message
);

public sealed record ContactResponse(
    string Id,
    string FullName,
    string Email,
    string? Phone,
    string InquiryType,
    string Message,
    string Status,
    string NotificationStatus,
    DateTime SubmittedAt,
    DateTime? UpdatedAt,
    string? HandledBy
);

public sealed record UpdateContactStatusRequest(
    [Required]
    string Status
);
