using Microsoft.AspNetCore.Identity;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Infrastructure.Security;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, n.d. PasswordHasher<TUser> Class (Microsoft.AspNetCore.Identity) [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//OWASP Foundation, n.d. Authentication - OWASP Cheat Sheet Series [Online guide].
//Available at: <https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class PasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<User> Inner = new();
    private static readonly User Dummy = new();

    public string Hash(string plaintext) => Inner.HashPassword(Dummy, plaintext);

    public bool Verify(string hash, string plaintext) =>
        Inner.VerifyHashedPassword(Dummy, hash, plaintext) != PasswordVerificationResult.Failed;
}
