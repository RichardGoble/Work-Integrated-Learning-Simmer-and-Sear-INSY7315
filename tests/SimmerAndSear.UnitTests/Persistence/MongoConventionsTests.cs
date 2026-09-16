using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Shouldly;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.UnitTests.Persistence;

/// <summary>
/// Guards the BSON mapping. Each of these catches a bug that is silent in
/// development and expensive in production: money quietly stored as a double,
/// timestamps that drift by the local UTC offset, field names that match nothing
/// in the documented schema, and a duplicate registration that kills the
/// integration suite on its second test.
/// </summary>
public sealed class MongoConventionsTests
{
    public MongoConventionsTests() => MongoConventions.Register();

    private static Product SampleProduct() => new()
    {
        Id = ObjectId.GenerateNewId().ToString(),
        Name = "Cast Iron Skillet 28cm",
        Sku = "SS-CI-28",
        Price = 1234.56m,
        Currency = "ZAR",
        CreatedAt = DateTime.UtcNow,
        Images = [new ProductImage { AltText = "Skillet", IsPrimary = true }]
    };

    [Fact]
    public void Money_is_stored_as_decimal128_never_as_double()
    {
        var bson = SampleProduct().ToBsonDocument();

        // A double here would mean order totals accumulate rounding error.
        bson["price"].BsonType.ShouldBe(BsonType.Decimal128);
    }

    [Fact]
    public void Money_survives_a_round_trip_exactly()
    {
        var original = SampleProduct();

        var restored = BsonSerializer.Deserialize<Product>(original.ToBsonDocument());

        restored.Price.ShouldBe(1234.56m);
    }

    [Fact]
    public void Field_names_are_camel_case_to_match_the_documented_schema()
    {
        var bson = SampleProduct().ToBsonDocument();

        bson.Contains("price").ShouldBeTrue();
        bson.Contains("Price").ShouldBeFalse();
        bson.Contains("stockQuantity").ShouldBeTrue();
    }

    [Fact]
    public void Dates_round_trip_as_utc()
    {
        var restored = BsonSerializer.Deserialize<Product>(SampleProduct().ToBsonDocument());

        // DateTimeKind.Unspecified here is how an audit log silently becomes
        // untrustworthy: entries drift by the host's UTC offset.
        restored.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Enums_are_stored_as_names_not_ordinals()
    {
        var entry = AuditLogEntry.Create(
            actorId: ObjectId.GenerateNewId().ToString(),
            actorName: "Master Admin",
            actorRole: RoleName.MasterAdmin,
            action: AuditAction.AccessDenied,
            entityType: nameof(Product),
            entityId: null,
            summary: "Refused catalogue edit.");

        var bson = entry.ToBsonDocument();

        // Ordinals would mean reordering the enum silently rewrites history.
        bson["action"].AsString.ShouldBe(nameof(AuditAction.AccessDenied));
        bson["actorRole"].AsString.ShouldBe(nameof(RoleName.MasterAdmin));
    }

    [Fact]
    public void Site_settings_keeps_its_literal_id_rather_than_an_object_id()
    {
        var bson = new SiteSettings { SalesNotificationEmail = "sales@example.com" }.ToBsonDocument();

        // Giving the singleton an ObjectId representation would throw on first write.
        bson["_id"].AsString.ShouldBe(SiteSettings.SingletonId);
    }

    [Fact]
    public void An_entity_that_has_not_been_inserted_yet_is_still_serialisable()
    {
        // A new entity's Id is empty until the driver's id generator fills it in
        // on insert. Serialising one before that - in a test, a log line, or a
        // bulk-write builder - must not throw a FormatException from inside the
        // driver.
        var fresh = new Product
        {
            Name = "Unsaved",
            Sku = "SS-NEW-01",
            Price = 10m,
            Images = [new ProductImage { AltText = "Unsaved" }]
        };

        Should.NotThrow(() => fresh.ToBsonDocument());
    }

    [Fact]
    public void Register_is_idempotent()
    {
        // WebApplicationFactory builds several hosts per integration-test run.
        // Without the guard, the second registration throws and the suite dies.
        Should.NotThrow(() =>
        {
            MongoConventions.Register();
            MongoConventions.Register();
        });
    }
}
