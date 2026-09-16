using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Infrastructure.Persistence;

/// <summary>
/// BSON mapping for the whole application.
///
/// <para>
/// <b>Call <see cref="Register"/> before anything touches a model.</b> The driver
/// builds and caches a class map the first time it serialises a type; conventions
/// registered after that point are silently ignored, and the database quietly
/// fills with PascalCase field names that match nothing in the Task 1 schemas.
/// The first line of Program.cs and of the seeder, and nowhere else.
/// </para>
///
/// <para>
/// Mapping is configured here, in the infrastructure layer, rather than with
/// attributes on the domain entities. That keeps the domain free of any storage
/// concern - which ArchitectureTests asserts - without the cost of a parallel set
/// of persistence documents and hand-written mappers for every entity.
/// </para>
/// </summary>
public static class MongoConventions
{
    private static int _registered;

    /// <summary>
    /// Idempotent and thread-safe. WebApplicationFactory builds several hosts in a
    /// single integration-test run, and both RegisterSerializer and
    /// RegisterClassMap throw on a duplicate registration - so without this guard
    /// the suite dies on the second test.
    /// </summary>
    public static void Register()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1)
        {
            return;
        }

        var pack = new ConventionPack
        {
            // Field names match the JSON schemas documented in Task 1.
            new CamelCaseElementNameConvention(),

            // A document written by a newer build must not break an older one.
            new IgnoreExtraElementsConvention(true),

            // Enums stored as their names. Readable in Compass, and a reordered
            // enum cannot silently change the meaning of existing documents the
            // way an ordinal would.
            new EnumRepresentationConvention(BsonType.String),

            new IgnoreIfNullConvention(true)
        };

        ConventionRegistry.Register("simmerandsear", pack, _ => true);

        // Every DateTime round-trips as UTC. Without this the driver returns
        // DateTimeKind.Unspecified and local/UTC drift silently corrupts the
        // audit log - the one collection that must be trustworthy.
        BsonSerializer.RegisterSerializer(new DateTimeSerializer(DateTimeKind.Utc));

        // Money. Without an explicit representation the driver stores decimal as
        // a double and order totals accumulate rounding error.
        BsonSerializer.RegisterSerializer(typeof(decimal),
            new DecimalSerializer(BsonType.Decimal128));
        BsonSerializer.RegisterSerializer(typeof(decimal?),
            new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));

        RegisterClassMaps();
    }

    private static void RegisterClassMaps()
    {
        // Identifiers are strings in the domain and real ObjectIds in the database.
        MapWithObjectId<Product>();
        MapWithObjectId<User>();
        MapWithObjectId<MediaAsset>();
        MapWithObjectId<AuditLogEntry>();

        // SiteSettings is the exception: its id is the fixed literal "global", so
        // it is stored as a plain string. Giving it an ObjectId representation
        // would throw on the first write.
        MapWithStringId<SiteSettings>();
    }

    private static void MapWithObjectId<T>() where T : class
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(T)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<T>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);

            var id = typeof(T).GetProperty("Id")
                     ?? throw new InvalidOperationException($"{typeof(T).Name} has no Id property.");

            // The id generator fills in the identifier on insert and writes it
            // back onto the entity, so a repository can return it. The serializer
            // additionally tolerates an unassigned id, so an entity that has not
            // been inserted yet is still serialisable.
            cm.MapIdMember(id)
              .SetSerializer(new ObjectIdStringSerializer())
              .SetIdGenerator(StringObjectIdGenerator.Instance);
        });
    }

    private static void MapWithStringId<T>() where T : class
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(T)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<T>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);

            var id = typeof(T).GetProperty("Id")
                     ?? throw new InvalidOperationException($"{typeof(T).Name} has no Id property.");

            cm.MapIdMember(id).SetSerializer(new StringSerializer(BsonType.String));
        });
    }
}
