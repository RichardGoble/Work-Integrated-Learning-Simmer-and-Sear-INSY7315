using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Infrastructure.Persistence;

/// <summary>
/// Creates collections, applies $jsonSchema validators, and builds indexes at
/// start-up. Idempotent, so it runs safely on every boot and in every environment.
///
/// <para>
/// Indexes and constraints as code, reviewable in one file, is deliberate: a
/// document database with no indexes and no validation is the default state most
/// projects ship in, and it forfeits the data-integrity and query-efficiency
/// marks entirely.
/// </para>
/// </summary>
public sealed class MongoSchemaInitializer(
    IMongoDatabase database,
    IOptions<MongoOptions> options,
    ILogger<MongoSchemaInitializer> logger) : IHostedService
{
    private const int NamespaceExists = 48;

    private static readonly Collation CaseInsensitive =
        new("en", strength: CollationStrength.Secondary);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.ApplySchemaOnStartup)
        {
            logger.LogInformation("Schema initialisation skipped by configuration.");
            return;
        }

        await EnsureCollectionsAsync(cancellationToken);
        await EnsureValidatorsAsync(cancellationToken);
        await EnsureIndexesAsync(cancellationToken);

        logger.LogInformation("Mongo collections, validators and indexes verified.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // ---------------------------------------------------------------- collections

    private async Task EnsureCollectionsAsync(CancellationToken ct)
    {
        foreach (var name in new[]
                 {
                     MongoCollections.Products,
                     MongoCollections.Users,
                     MongoCollections.AuditLog,
                     MongoCollections.SiteSettings,
                     MongoCollections.MediaAssets
                 })
        {
            try
            {
                await database.CreateCollectionAsync(name, cancellationToken: ct);
            }
            catch (MongoCommandException ex) when (ex.Code == NamespaceExists)
            {
                // Already there. collMod below still applies the current validator.
            }
        }
    }

    // ---------------------------------------------------------------- validators

    private async Task EnsureValidatorsAsync(CancellationToken ct)
    {
        // A product must carry at least one image. The rule comes straight from
        // the client's requirements, and it is enforced in the domain, again at
        // the API boundary, and here in the database - so no code path, migration
        // or console session can create a listing that breaks it.
        await ApplyValidatorAsync(MongoCollections.Products, new BsonDocument
        {
            { "bsonType", "object" },
            { "required", new BsonArray { "name", "sku", "price", "images", "createdAt" } },
            {
                "properties", new BsonDocument
                {
                    { "name", Str(minLength: 1) },
                    { "sku", Str(minLength: 1) },
                    { "price", new BsonDocument { { "bsonType", "decimal" }, { "minimum", 0 } } },
                    { "stockQuantity", new BsonDocument { { "bsonType", "int" }, { "minimum", 0 } } },
                    { "images", new BsonDocument { { "bsonType", "array" }, { "minItems", 1 } } },
                    { "isPublished", new BsonDocument("bsonType", "bool") }
                }
            }
        }, ct);

        await ApplyValidatorAsync(MongoCollections.Users, new BsonDocument
        {
            { "bsonType", "object" },
            { "required", new BsonArray { "email", "passwordHash", "role", "userType", "createdAt" } },
            {
                "properties", new BsonDocument
                {
                    // Format-checked here as well as by FluentValidation: the store
                    // should not depend on the application to stay coherent.
                    { "email", new BsonDocument { { "bsonType", "string" }, { "pattern", @"^\S+@\S+\.\S+$" } } },
                    { "passwordHash", Str(minLength: 1) },
                    { "isActive", new BsonDocument("bsonType", "bool") }
                }
            }
        }, ct);

        // The append-only guarantee, restated at the storage layer.
        await ApplyValidatorAsync(MongoCollections.AuditLog, new BsonDocument
        {
            { "bsonType", "object" },
            { "required", new BsonArray { "actorId", "actorRole", "action", "entityType", "occurredAt" } },
            {
                "properties", new BsonDocument
                {
                    { "actorId", Str(minLength: 1) },
                    { "entityType", Str(minLength: 1) },
                    { "occurredAt", new BsonDocument("bsonType", "date") }
                }
            }
        }, ct);

        await ApplyValidatorAsync(MongoCollections.MediaAssets, new BsonDocument
        {
            { "bsonType", "object" },
            { "required", new BsonArray { "fileName", "contentType", "gridFsId", "uploadedAt" } },
            {
                "properties", new BsonDocument
                {
                    { "fileSizeBytes", new BsonDocument { { "bsonType", "long" }, { "minimum", 0 } } }
                }
            }
        }, ct);
    }

    private static BsonDocument Str(int minLength) =>
        new() { { "bsonType", "string" }, { "minLength", minLength } };

    private async Task ApplyValidatorAsync(string collection, BsonDocument schema, CancellationToken ct)
    {
        await database.RunCommandAsync<BsonDocument>(new BsonDocument
        {
            { "collMod", collection },
            { "validator", new BsonDocument("$jsonSchema", schema) },
            // A fresh database has no legacy documents, so nothing needs grandfathering.
            { "validationLevel", "strict" },
            { "validationAction", "error" }
        }, cancellationToken: ct);
    }

    // ---------------------------------------------------------------- indexes

    private async Task EnsureIndexesAsync(CancellationToken ct)
    {
        var products = database.GetCollection<Product>(MongoCollections.Products);

        await products.Indexes.CreateManyAsync(
        [
            // Serves the catalogue filter and sort in one index. Collated, so
            // brand and category compare case-insensitively - queries must pass
            // the same collation to use it.
            new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys
                    .Ascending(p => p.Category)
                    .Ascending(p => p.Brand)
                    .Ascending(p => p.Price),
                new CreateIndexOptions { Name = "ix_products_category_brand_price", Collation = CaseInsensitive }),

            // Free-text search across name and description. Only ONE text index is
            // permitted per collection, and a text index cannot take a collation.
            new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys.Text(p => p.Name).Text(p => p.Description),
                new CreateIndexOptions { Name = "tx_products_search" }),

            new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys.Ascending(p => p.Sku),
                new CreateIndexOptions { Name = "ux_products_sku", Unique = true }),

            new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys.Ascending(p => p.IsPublished).Descending(p => p.CreatedAt),
                new CreateIndexOptions { Name = "ix_products_published_created" })
        ], ct);

        var users = database.GetCollection<User>(MongoCollections.Users);

        await users.Indexes.CreateManyAsync(
        [
            // Collated and unique: "Bob@example.com" and "bob@example.com" cannot
            // both be registered, which is an account-takeover vector otherwise.
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Name = "ux_users_email", Unique = true, Collation = CaseInsensitive }),

            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Role).Ascending(u => u.IsActive),
                new CreateIndexOptions { Name = "ix_users_role_active" })
        ], ct);

        var audit = database.GetCollection<AuditLogEntry>(MongoCollections.AuditLog);

        await audit.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<AuditLogEntry>(
                Builders<AuditLogEntry>.IndexKeys.Descending(a => a.OccurredAt),
                new CreateIndexOptions { Name = "ix_audit_occurred" }),

            new CreateIndexModel<AuditLogEntry>(
                Builders<AuditLogEntry>.IndexKeys
                    .Ascending(a => a.EntityType)
                    .Ascending(a => a.EntityId)
                    .Descending(a => a.OccurredAt),
                new CreateIndexOptions { Name = "ix_audit_entity" }),

            new CreateIndexModel<AuditLogEntry>(
                Builders<AuditLogEntry>.IndexKeys.Ascending(a => a.ActorId).Descending(a => a.OccurredAt),
                new CreateIndexOptions { Name = "ix_audit_actor" })
        ], ct);
    }
}
