using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Infrastructure.Persistence;
// ── References ──────────────────────────────────────────────────────────────
//MongoDB, n.d. Schema Validation [Source code].
//Available at: <https://www.mongodb.com/docs/manual/core/schema-validation/> [Accessed 18 September 2026].
//
//MongoDB, n.d. Create and Manage Indexes [Source code].
//Available at: <https://www.mongodb.com/docs/drivers/csharp/current/indexes/> [Accessed 18 September 2026].
//
//MongoDB, n.d. Run a Database Command [Source code].
//Available at: <https://www.mongodb.com/docs/drivers/csharp/current/run-command/> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class MongoSchemaInitializer(
    IMongoDatabase database,
    IOptions<MongoOptions> options,
    SchemaStatus status,
    ILogger<MongoSchemaInitializer> logger) : IHostedService
{
    private const int NamespaceExists = 48;
    private const int Unauthorized = 13;

    private static readonly Collation CaseInsensitive =
        new("en", strength: CollationStrength.Secondary);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.ApplySchemaOnStartup)
        {
            const string reason = "Disabled by Mongo:ApplySchemaOnStartup.";
            status.MarkSkipped(reason);
            logger.LogInformation("Schema initialisation skipped. {Reason}", reason);
            return;
        }

        try
        {
            await EnsureCollectionsAsync(cancellationToken);
            await EnsureValidatorsAsync(cancellationToken);
            await EnsureIndexesAsync(cancellationToken);

            status.MarkApplied();
            logger.LogInformation("Mongo collections, validators and indexes verified.");
        }
        catch (MongoCommandException ex) when (ex.Code == Unauthorized)
        {
        
            var reason =
                $"The database user is not permitted to apply schema: {ex.Message} " +
                "Grant the collMod action (a custom role of readWrite + collMod, or dbAdmin) " +
                "on this database, or set Mongo:ApplySchemaOnStartup to false and apply the " +
                "schema separately.";

            status.MarkFailed(reason);
            logger.LogError(ex, "Schema initialisation refused. {Reason}", reason);
        }
        catch (Exception ex)
        {
            status.MarkFailed($"Schema initialisation failed: {ex.Message}");
            logger.LogError(ex, "Schema initialisation failed. The application is still serving, " +
                                "but the database may not be enforcing every constraint.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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
            }
        }
    }

    private async Task EnsureValidatorsAsync(CancellationToken ct)
    {
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
                    { "email", new BsonDocument { { "bsonType", "string" }, { "pattern", @"^\S+@\S+\.\S+$" } } },
                    { "passwordHash", Str(minLength: 1) },
                    { "isActive", new BsonDocument("bsonType", "bool") }
                }
            }
        }, ct);
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
            { "validationLevel", "strict" },
            { "validationAction", "error" }
        }, cancellationToken: ct);
    }

    private async Task EnsureIndexesAsync(CancellationToken ct)
    {
        var products = database.GetCollection<Product>(MongoCollections.Products);

        await products.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys
                    .Ascending(p => p.Category)
                    .Ascending(p => p.Brand)
                    .Ascending(p => p.Price),
                new CreateIndexOptions { Name = "ix_products_category_brand_price", Collation = CaseInsensitive }),
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
