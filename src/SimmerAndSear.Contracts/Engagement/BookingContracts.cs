using System.ComponentModel.DataAnnotations;

namespace SimmerAndSear.Contracts.Engagement;

public sealed record CreateBookingRequest(
    [StringLength(120)]
    string? FullName,

    [EmailAddress, StringLength(200)]
    string? Email,

    [StringLength(30)]
    string? Phone,

    [Required]
    DateTime PreferredDateTime,

    [Required, StringLength(500, MinimumLength = 2)]
    string Purpose,

    [StringLength(100)]
    string? OrderId
);

public sealed record BookingResponse(
    string Id,
    string? ClientId,
    string ClientName,
    string ClientEmail,
    string? ClientPhone,
    DateTime PreferredDateTime,
    string Purpose,
    string? OrderId,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed record UpdateBookingStatusRequest(
    [Required]
    string Status
);
