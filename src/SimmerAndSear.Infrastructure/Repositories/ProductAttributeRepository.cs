using MongoDB.Bson;
using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Catalogue;

namespace SimmerAndSear.Infrastructure.Repositories;

public sealed class ProductAttributeRepository(IMongoDatabase database) : IProductAttributeRepository
{
    private readonly IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("productAttributes");

    public async Task<CatalogueFacets> GetAsync(CancellationToken ct = default)
    {
        var entries = await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync(ct);
        string[] Values(string field) => entries.Where(e => e["field"] == field)
            .Select(e => e["value"].AsString).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        return new(Values("brand"), Values("category"), Values("cookwareType"), Values("material"), Values("sizeLabel"));
    }

    public async Task AddAsync(string field, string value, CancellationToken ct = default)
    {
        var id = field + ":" + value.ToUpperInvariant();
        await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", id),
            Builders<BsonDocument>.Update.SetOnInsert("field", field).SetOnInsert("value", value),
            new UpdateOptions { IsUpsert = true }, ct);
    }
}
