namespace SimmerAndSear.Application.Engagement;

public interface IContactNotificationQueue
{
    ValueTask EnqueueAsync(ContactNotificationJob job, CancellationToken ct = default);
    IAsyncEnumerable<ContactNotificationJob> ReadAllAsync(CancellationToken ct = default);
}
