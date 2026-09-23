using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Application.Abstractions;

public interface IBookingRepository
{
    Task<Booking?> GetAsync(string id, CancellationToken ct = default);
    Task<List<Booking>> ListAsync(CancellationToken ct = default);
    Task<List<Booking>> ListForClientAsync(string clientId, CancellationToken ct = default);
    Task<string> CreateAsync(Booking booking, CancellationToken ct = default);
    Task UpdateStatusAsync(string id, BookingStatus status, CancellationToken ct = default);
}
