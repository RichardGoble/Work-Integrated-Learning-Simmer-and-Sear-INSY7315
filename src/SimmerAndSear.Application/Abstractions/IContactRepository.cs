using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Application.Abstractions;

public interface IContactRepository
{
    Task<ContactRequest?> GetAsync(string id, CancellationToken ct = default);
    Task<List<ContactRequest>> ListAsync(CancellationToken ct = default);
    Task<string> CreateAsync(ContactRequest request, CancellationToken ct = default);
    Task UpdateStatusAsync(string id, ContactStatus status, string handledBy, CancellationToken ct = default);
    Task UpdateNotificationStateAsync(string id, ContactNotificationStatus status, string? error, DateTime? sentAt, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
