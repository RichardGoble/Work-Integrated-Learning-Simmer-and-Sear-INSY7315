using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.Infrastructure.Repositories;

public sealed class BookingRepository(IMongoDatabase database)
    : MongoRepositoryBase<Booking>(database, MongoCollections.Bookings), IBookingRepository
{
    public async Task<Booking?> GetAsync(string id, CancellationToken ct = default)
    {
        if (!IsValidId(id)) return null;
        return await Collection.Find(Filter.Eq(b => b.Id, id)).FirstOrDefaultAsync(ct);
    }

    public async Task<List<Booking>> ListAsync(CancellationToken ct = default) =>
        await Collection.Find(Filter.Empty).Sort(SortBy.Descending(b => b.CreatedAt)).ToListAsync(ct);

    public async Task<List<Booking>> ListForClientAsync(string clientId, CancellationToken ct = default) =>
        await Collection.Find(Filter.Eq(b => b.ClientId, clientId)).Sort(SortBy.Descending(b => b.CreatedAt)).ToListAsync(ct);

    public async Task<string> CreateAsync(Booking booking, CancellationToken ct = default)
    {
        if (booking.CreatedAt == default) booking.CreatedAt = DateTime.UtcNow;
        if (booking.UpdatedAt == default) booking.UpdatedAt = booking.CreatedAt;
        await Collection.InsertOneAsync(booking, cancellationToken: ct);
        return booking.Id;
    }

    public async Task UpdateStatusAsync(string id, BookingStatus status, CancellationToken ct = default)
    {
        if (!IsValidId(id)) return;
        var update = Update
            .Set(b => b.Status, status)
            .Set(b => b.UpdatedAt, DateTime.UtcNow);
        await Collection.UpdateOneAsync(Filter.Eq(b => b.Id, id), update, cancellationToken: ct);
    }
}
