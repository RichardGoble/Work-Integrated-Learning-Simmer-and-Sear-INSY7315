using System.Threading.Channels;
using SimmerAndSear.Application.Engagement;

namespace SimmerAndSear.Infrastructure.Engagement;

public sealed class ContactNotificationQueue : IContactNotificationQueue
{
    private readonly Channel<ContactNotificationJob> _channel =
        Channel.CreateUnbounded<ContactNotificationJob>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask EnqueueAsync(ContactNotificationJob job, CancellationToken ct = default)
    {
        return _channel.Writer.WriteAsync(job, ct);
    }

    public IAsyncEnumerable<ContactNotificationJob> ReadAllAsync(CancellationToken ct = default)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }
}
