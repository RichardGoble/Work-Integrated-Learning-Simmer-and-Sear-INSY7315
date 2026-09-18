namespace SimmerAndSear.Infrastructure.Persistence;
public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    public string ConnectionString { get; set; } = string.Empty;

    public string Database { get; set; } = string.Empty;
    public string FileBucket { get; set; } = "media";
    public bool ApplySchemaOnStartup { get; set; } = true;
}
