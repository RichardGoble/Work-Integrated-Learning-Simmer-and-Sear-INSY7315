using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.Infrastructure.Repositories;
//References
//MongoDB, n.d. Insert Documents [Source code].
//Available at: <https://www.mongodb.com/docs/drivers/csharp/current/crud/insert/> [Accessed 21 September 2026].

//MongoDB, n.d. Unique Indexes [Source code].
//Available at: <https://www.mongodb.com/docs/manual/core/index-unique/> [Accessed 21 September 2026].

public sealed class QuotationDocumentRepository(IMongoDatabase database)
    : MongoRepositoryBase<QuotationDocument>(database, MongoCollections.QuotationDocuments), IQuotationDocumentRepository
{
    public async Task<QuotationDocument?> GetByOrderAsync(string orderId, CancellationToken ct = default) =>
        !IsValidId(orderId)
            ? null
            : await Collection.Find(Filter.Eq(d => d.OrderId, orderId)).Limit(1).FirstOrDefaultAsync(ct);

    public async Task<bool> TryCreateAsync(QuotationDocument document, CancellationToken ct = default)
    {
        try { await Collection.InsertOneAsync(document, cancellationToken: ct); }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
        return true;
    }
}
