namespace SimmerAndSear.Infrastructure.Persistence;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Options pattern [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/core/extensions/options> [Accessed 18 September 2026].
//
//Microsoft, 2025. Properties [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    public string ConnectionString { get; set; } = string.Empty;

    public string Database { get; set; } = string.Empty;
    public string FileBucket { get; set; } = "media";
    public bool ApplySchemaOnStartup { get; set; } = true;
}
