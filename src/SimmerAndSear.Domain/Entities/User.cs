using SimmerAndSear.Domain.Abstractions;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Domain.Entities;

/// <summary>
/// An account. Identity is managed by this application rather than an external
/// provider, so the password is held only as a PBKDF2 hash produced by
/// Microsoft's PasswordHasher; the plaintext is never stored or logged.
/// </summary>
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

    /// <summary>Drives lockout, which satisfies the "rate-limiting on failed logins" NFR.</summary>
    public int FailedLoginAttempts { get; set; }

    public DateTime? LockedOutUntil { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }

    /// <summary>Customer-only fields. Null for staff accounts.</summary>
    public ClientProfile? Client { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public bool IsLockedOut(DateTime utcNow) => LockedOutUntil is { } until && until > utcNow;
}

/// <summary>Embedded rather than joined - these fields are never read alone.</summary>
public sealed class ClientProfile
{
    public string? ShippingAddress { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Province { get; set; }
    public bool MarketingOptIn { get; set; }
}
