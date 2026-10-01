using SimmerAndSear.Domain.Abstractions;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Domain.Entities;

public sealed class Booking : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string? ClientPhone { get; set; }
    public DateTime PreferredDateTime { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Requested;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
