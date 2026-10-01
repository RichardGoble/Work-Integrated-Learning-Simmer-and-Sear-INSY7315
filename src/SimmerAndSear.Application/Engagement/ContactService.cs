using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Engagement;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Application.Engagement;

public sealed class ContactService(
    IContactRepository contactRepo,
    IContactNotificationQueue notificationQueue)
{
    public async Task<ContactResponse> SubmitAsync(CreateContactRequest request, CancellationToken ct = default)
    {
        var entity = new ContactRequest
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            InquiryType = string.IsNullOrWhiteSpace(request.InquiryType) ? "General Inquiry" : request.InquiryType.Trim(),
            Message = request.Message.Trim(),
            Status = ContactStatus.New,
            NotificationStatus = ContactNotificationStatus.Pending,
            SubmittedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 1. SAVE ContactRequest TO MONGODB FIRST
        await contactRepo.CreateAsync(entity, ct);

        // 2. Enqueue notification job (HTTP request NEVER awaits Brevo directly)
        try
        {
            var job = new ContactNotificationJob(
                entity.Id,
                entity.FullName,
                entity.Email,
                entity.Phone,
                entity.InquiryType,
                entity.Message,
                entity.SubmittedAt
            );
            await notificationQueue.EnqueueAsync(job, ct);
        }
        catch
        {
            // If enqueueing unexpectedly fails, the ContactRequest MUST still exist in MongoDB
        }

        return Map(entity);
    }

    public async Task<List<ContactResponse>> ListAsync(CancellationToken ct = default)
    {
        var items = await contactRepo.ListAsync(ct);
        return items.Select(Map).ToList();
    }

    public async Task<ContactResponse?> GetAsync(string id, CancellationToken ct = default)
    {
        var entity = await contactRepo.GetAsync(id, ct);
        return entity is null ? null : Map(entity);
    }

    public async Task<ContactResponse?> UpdateStatusAsync(
        string id, ContactStatus newStatus, string staffId, CancellationToken ct = default)
    {
        var existing = await contactRepo.GetAsync(id, ct);
        if (existing is null) return null;

        await contactRepo.UpdateStatusAsync(id, newStatus, staffId, ct);

        existing.Status = newStatus;
        existing.HandledBy = staffId;
        existing.UpdatedAt = DateTime.UtcNow;

        return Map(existing);
    }

    private static ContactResponse Map(ContactRequest c) => new(
        c.Id,
        c.FullName,
        c.Email,
        c.Phone,
        c.InquiryType,
        c.Message,
        c.Status.ToString(),
        c.NotificationStatus.ToString(),
        c.SubmittedAt,
        c.UpdatedAt,
        c.HandledBy
    );
}
