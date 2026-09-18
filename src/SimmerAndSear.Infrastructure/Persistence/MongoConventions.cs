using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Infrastructure.Persistence;
public static class MongoConventions
{
    private static int _registered;
    public static void Register()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1)
        {
            return;
        }

        var pack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true),
            new EnumRepresentationConvention(BsonType.String),

            new IgnoreIfNullConvention(true)
        };

        ConventionRegistry.Register("simmerandsear", pack, _ => true);
        BsonSerializer.RegisterSerializer(new DateTimeSerializer(DateTimeKind.Utc));
        BsonSerializer.RegisterSerializer(typeof(decimal),
            new DecimalSerializer(BsonType.Decimal128));
        BsonSerializer.RegisterSerializer(typeof(decimal?),
            new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));

        RegisterClassMaps();
    }

    private static void RegisterClassMaps()
    {
        MapWithObjectId<Product>();
        MapWithObjectId<User>();
        MapWithObjectId<MediaAsset>();
        MapWithObjectId<AuditLogEntry>();
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
