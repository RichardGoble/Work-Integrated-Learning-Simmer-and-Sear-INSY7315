namespace SimmerAndSear.Infrastructure.Persistence;

/// <summary>
/// Bound from the "Mongo" configuration section.
/// <para>
/// On Linux App Service the nested keys use a double underscore, not a colon:
/// <c>Mongo__ConnectionString</c>, <c>Mongo__Database</c>. The connection string
/// is a secret and lives in user-secrets locally, App Settings in Azure, and
/// GitHub Secrets in CI - never in the repository.
/// </para>
/// </summary>
public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    public string ConnectionString { get; set; } = string.Empty;

    public string Database { get; set; } = string.Empty;

    /// <summary>GridFS bucket holding product and post images, and generated PDFs.</summary>
    public string FileBucket { get; set; } = "media";

    /// <summary>
    /// Whether to create indexes and schema validators at start-up. On by default;
    /// turn it off only if a deployment must not touch the schema.
    /// </summary>
    public bool ApplySchemaOnStartup { get; set; } = true;
}
