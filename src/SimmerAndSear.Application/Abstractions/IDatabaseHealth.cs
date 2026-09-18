namespace SimmerAndSear.Application.Abstractions;
public interface IDatabaseHealth
{
    Task<bool> IsReachableAsync(CancellationToken ct = default);
}
