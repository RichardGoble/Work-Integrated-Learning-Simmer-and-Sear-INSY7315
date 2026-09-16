namespace SimmerAndSear.Domain.Abstractions;

/// <summary>
/// Anything with a persistent identity. The identifier is a string in the domain
/// so that no storage concern (MongoDB's ObjectId) leaks into this layer.
/// </summary>
public interface IEntity
{
    string Id { get; }
}
