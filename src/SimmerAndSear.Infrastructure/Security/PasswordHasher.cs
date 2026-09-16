using Microsoft.AspNetCore.Identity;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Infrastructure.Security;

/// <summary>
/// Wraps Microsoft's <see cref="PasswordHasher{TUser}"/>: PBKDF2-HMAC-SHA256,
/// 100,000 iterations, a 128-bit per-user salt, and a constant-time comparison.
///
/// <para>
/// Identity is managed by this application rather than an external provider, so
/// this is the one place passwords are handled. Deliberately not hand-rolled -
/// the algorithm, the iteration count and the versioned hash format are
/// Microsoft's, and the wrapper exists only to keep the rest of the code free of
/// an ASP.NET Identity dependency.
/// </para>
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<User> Inner = new();
    private static readonly User Dummy = new();

    public string Hash(string plaintext) => Inner.HashPassword(Dummy, plaintext);

    public bool Verify(string hash, string plaintext) =>
        Inner.VerifyHashedPassword(Dummy, hash, plaintext) != PasswordVerificationResult.Failed;
}
