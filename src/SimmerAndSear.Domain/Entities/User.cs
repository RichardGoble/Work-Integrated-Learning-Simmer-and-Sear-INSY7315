using SimmerAndSear.Domain.Abstractions;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Domain.Entities;
public sealed class User : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }

    public UserType UserType { get; set; } = UserType.Client;
    public RoleName Role { get; set; } = RoleName.Customer;

    public bool IsActive { get; set; } = true;
    public int FailedLoginAttempts { get; set; }

    public DateTime? LockedOutUntil { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public ClientProfile? Client { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public bool IsLockedOut(DateTime utcNow) => LockedOutUntil is { } until && until > utcNow;
}
public sealed class ClientProfile
{
    public string? ShippingAddress { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Province { get; set; }
    public bool MarketingOptIn { get; set; }
}
