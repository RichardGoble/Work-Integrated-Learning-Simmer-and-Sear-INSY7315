namespace SimmerAndSear.Application.Abstractions;

/// <summary>
/// Password hashing, kept behind an interface so the application layer never
/// depends on a particular implementation - and so the seeder, the registration
/// endpoint and the login endpoint provably use the same one.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string plaintext);

    /// <summary>
    /// Verifies a candidate password.
    /// <para>
    /// Callers must report the same failure for an unknown email and a wrong
    /// password. Distinguishing them turns the login form into a user-enumeration
    /// oracle.
    /// </para>
    /// </summary>
    bool Verify(string hash, string plaintext);
}
