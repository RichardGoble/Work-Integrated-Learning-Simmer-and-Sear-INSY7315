using SimmerAndSear.Domain.Abstractions;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Domain.Entities;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. C# classes [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/classes> [Accessed 18 September 2026].
//
//OWASP Foundation, n.d. Authentication - OWASP Cheat Sheet Series [Online guide].
//Available at: <https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
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
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. C# classes [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/classes> [Accessed 18 September 2026].
//
//Microsoft, 2025. Properties [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class ClientProfile
{
    public string? ShippingAddress { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Province { get; set; }
    public bool MarketingOptIn { get; set; }
}
