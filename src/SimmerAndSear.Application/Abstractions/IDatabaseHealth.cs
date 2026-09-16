namespace SimmerAndSear.Application.Abstractions;

/// <summary>
/// Lets the readiness probe prove the database is reachable without the web layer
/// ever naming a MongoDB type. ArchitectureTests asserts exactly that, so this
/// small abstraction is what keeps the health endpoint on the right side of the
/// layering rule.
/// </summary>
public interface IDatabaseHealth
{
    Task<bool> IsReachableAsync(CancellationToken ct = default);
}
