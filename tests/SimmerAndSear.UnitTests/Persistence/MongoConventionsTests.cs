using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Shouldly;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.UnitTests.Persistence;
// ── References ──────────────────────────────────────────────────────────────
//MongoDB, n.d. Serialization [Source code].
//Available at: <https://www.mongodb.com/docs/drivers/csharp/current/serialization/> [Accessed 18 September 2026].
//
//xUnit.net, 2025. Getting Started with xUnit.net v2 [Source code].
//Available at: <https://xunit.net/docs/getting-started/v2/getting-started> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
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
        bson["action"].AsString.ShouldBe(nameof(AuditAction.AccessDenied));
        bson["actorRole"].AsString.ShouldBe(nameof(RoleName.MasterAdmin));
    }

    [Fact]
    public void Site_settings_keeps_its_literal_id_rather_than_an_object_id()
    {
        var bson = new SiteSettings { SalesNotificationEmail = "sales@example.com" }.ToBsonDocument();
        bson["_id"].AsString.ShouldBe(SiteSettings.SingletonId);
    }

    [Fact]
    public void An_entity_that_has_not_been_inserted_yet_is_still_serialisable()
    {
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
        Should.NotThrow(() =>
        {
            MongoConventions.Register();
            MongoConventions.Register();
        });
    }
}
