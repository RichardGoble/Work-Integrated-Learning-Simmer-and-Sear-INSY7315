namespace SimmerAndSear.Application.Abstractions;


public enum SchemaState
{
    NotAttempted,
    Skipped,
    Applied,  
    Failed
}


public interface ISchemaStatus
{
    SchemaState State { get; }
    
    string? Detail { get; }
}
