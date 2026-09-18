namespace SimmerAndSear.Application.Abstractions;
public interface IPasswordHasher
{
    string Hash(string plaintext);
    bool Verify(string hash, string plaintext);
}
