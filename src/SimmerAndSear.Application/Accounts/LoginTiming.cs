using SimmerAndSear.Application.Abstractions;

namespace SimmerAndSear.Application.Accounts;

// ── References ──────────────────────────────────────────────────────────────
//OWASP Foundation, n.d. Authentication - OWASP Cheat Sheet Series [Online guide].
//Available at: <https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html> [Accessed 18 September 2026].
//
//Microsoft, n.d. PasswordHasher<TUser> Class (Microsoft.AspNetCore.Identity) [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class LoginTiming(IPasswordHasher hasher)
{
    public string DummyHash { get; } = hasher.Hash(Guid.NewGuid().ToString());
}
