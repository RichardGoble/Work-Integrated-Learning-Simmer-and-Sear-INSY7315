using SimmerAndSear.Application.Abstractions;

namespace SimmerAndSear.Application.Accounts;

public sealed class LoginTiming(IPasswordHasher hasher)
{
    public string DummyHash { get; } = hasher.Hash(Guid.NewGuid().ToString());
}
