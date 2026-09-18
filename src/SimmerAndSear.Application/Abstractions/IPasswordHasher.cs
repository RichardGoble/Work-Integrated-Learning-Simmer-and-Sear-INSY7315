namespace SimmerAndSear.Application.Abstractions;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 18 September 2026].
//
//Microsoft, n.d. PasswordHasher<TUser> Class (Microsoft.AspNetCore.Identity) [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public interface IPasswordHasher
{
    string Hash(string plaintext);
    bool Verify(string hash, string plaintext);
}
