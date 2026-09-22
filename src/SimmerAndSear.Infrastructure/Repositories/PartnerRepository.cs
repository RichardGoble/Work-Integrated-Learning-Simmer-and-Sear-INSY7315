using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.Infrastructure.Repositories;

public sealed class PartnerRepository(IMongoDatabase database)
    : MongoRepositoryBase<RetailPartner>(database, MongoCollections.RetailPartners), IPartnerRepository
{
    public async Task<List<RetailPartner>> ListAsync(CancellationToken ct = default) =>
        await Collection.Find(Filter.Empty).Sort(SortBy.Ascending(p => p.Name)).ToListAsync(ct);

    public async Task<string> CreateAsync(RetailPartner partner, CancellationToken ct = default)
    {
        partner.CreatedAt = DateTime.UtcNow;
        await Collection.InsertOneAsync(partner, cancellationToken: ct);
        return partner.Id;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        if (!IsValidId(id)) return;
        await Collection.DeleteOneAsync(Filter.Eq(p => p.Id, id), cancellationToken: ct);
    }
}
