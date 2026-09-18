using Microsoft.AspNetCore.Identity;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Infrastructure.Security;
public sealed class PasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<User> Inner = new();
    private static readonly User Dummy = new();

    public string Hash(string plaintext) => Inner.HashPassword(Dummy, plaintext);

    public bool Verify(string hash, string plaintext) =>
        Inner.VerifyHashedPassword(Dummy, hash, plaintext) != PasswordVerificationResult.Failed;
}
