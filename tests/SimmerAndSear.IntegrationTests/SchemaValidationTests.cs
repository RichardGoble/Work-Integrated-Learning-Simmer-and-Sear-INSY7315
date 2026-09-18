using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;

namespace SimmerAndSear.IntegrationTests;
public sealed class SchemaValidationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private IMongoCollection<BsonDocument> Products()
    {
        var scope = factory.Services.CreateScope();
        return scope.ServiceProvider
            .GetRequiredService<IMongoDatabase>()
            .GetCollection<BsonDocument>("products");
    }

    private static BsonDocument ProductWith(BsonArray images, string sku) => new()
    {
        { "name", "Test Pan" },
        { "sku", sku },
        { "price", new BsonDecimal128(100m) },
        { "stockQuantity", 1 },
        { "isPublished", true },
        { "images", images },
        { "createdAt", DateTime.UtcNow }
    };

    [Fact]
    public async Task The_database_refuses_a_product_with_no_images()
    {
        var products = Products();

        var act = async () => await products.InsertOneAsync(
            ProductWith([], "SS-TEST-NOIMG"));

        var error = await Should.ThrowAsync<MongoWriteException>(act);
        error.Message.ShouldContain("validation", Case.Insensitive);
    }

    [Fact]
    public async Task The_database_refuses_a_product_with_a_negative_price()
    {
        var products = Products();

        var document = ProductWith(
            [new BsonDocument { { "mediaAssetId", "x" }, { "altText", "A pan" } }],
            "SS-TEST-NEGPRICE");

        document["price"] = new BsonDecimal128(-1m);

        await Should.ThrowAsync<MongoWriteException>(
            async () => await products.InsertOneAsync(document));
    }

    [Fact]
    public async Task The_database_accepts_a_product_that_satisfies_the_rules()
    {
        var products = Products();
        const string sku = "SS-TEST-VALID";

        try
        {
            await products.InsertOneAsync(ProductWith(
                [new BsonDocument { { "mediaAssetId", "x" }, { "altText", "A pan" } }],
                sku));

            var found = await products
                .Find(Builders<BsonDocument>.Filter.Eq("sku", sku))
                .Limit(1)
                .FirstOrDefaultAsync();

            found.ShouldNotBeNull();
            found["price"].BsonType.ShouldBe(BsonType.Decimal128);
        }
        finally
        {
            await products.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("sku", sku));
        }
    }
}
