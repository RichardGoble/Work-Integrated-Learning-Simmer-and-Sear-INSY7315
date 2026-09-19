using SimmerAndSear.Application.Abstractions;

namespace SimmerAndSear.Infrastructure.Persistence;

public sealed class SchemaStatus : ISchemaStatus
{
    public SchemaState State { get; private set; } = SchemaState.NotAttempted;

    public string? Detail { get; private set; }

    public void MarkApplied()
    {
        State = SchemaState.Applied;
        Detail = null;
    }

    public void MarkSkipped(string reason)
    {
        State = SchemaState.Skipped;
        Detail = reason;
    }

    public void MarkFailed(string reason)
    {
        State = SchemaState.Failed;
        Detail = reason;
    }
}
