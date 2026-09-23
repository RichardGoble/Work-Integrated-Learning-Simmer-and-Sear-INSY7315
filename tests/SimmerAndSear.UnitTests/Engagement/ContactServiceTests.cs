using Shouldly;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Engagement;
using SimmerAndSear.Contracts.Engagement;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.UnitTests.Engagement;

public sealed class ContactServiceTests
{
    [Fact]
    public async Task SubmitAsync_valid_contact_persists_before_enqueueing_and_returns_success()
    {
        var repo = new MemoryContactRepository();
        var queue = new MemoryNotificationQueue();
        var service = new ContactService(repo, queue);

        var request = new CreateContactRequest(
            FullName: "Jane Doe",
            Email: "Jane@Example.com",
            Phone: "+27 82 111 2222",
            InquiryType: "Trade Inquiry",
            Message: "Interested in wholesale pricing."
        );

        var response = await service.SubmitAsync(request);

        response.ShouldNotBeNull();
        response.Id.ShouldNotBeNullOrWhiteSpace();
        response.FullName.ShouldBe("Jane Doe");
        response.Email.ShouldBe("jane@example.com");
        response.Status.ShouldBe(ContactStatus.New.ToString());
        response.NotificationStatus.ShouldBe(ContactNotificationStatus.Pending.ToString());

        repo.Items.Count.ShouldBe(1);
        var persisted = repo.Items.Single();
        persisted.Id.ShouldBe(response.Id);
        persisted.FullName.ShouldBe("Jane Doe");

        queue.EnqueuedJobs.Count.ShouldBe(1);
        queue.EnqueuedJobs[0].ContactRequestId.ShouldBe(response.Id);
        queue.EnqueuedJobs[0].Email.ShouldBe("jane@example.com");
    }

    [Fact]
    public async Task SubmitAsync_persists_contact_even_if_queue_throws_exception()
    {
        var repo = new MemoryContactRepository();
        var failingQueue = new FailingNotificationQueue();
        var service = new ContactService(repo, failingQueue);

        var request = new CreateContactRequest(
            FullName: "John Smith",
            Email: "john@example.com",
            Phone: null,
            InquiryType: "General",
            Message: "Hello Simmer & Sear"
        );

        var response = await service.SubmitAsync(request);

        response.ShouldNotBeNull();
        repo.Items.Count.ShouldBe(1);
        repo.Items.Single().FullName.ShouldBe("John Smith");
    }

    [Fact]
    public async Task UpdateStatusAsync_updates_status_handledBy_and_timestamp()
    {
        var repo = new MemoryContactRepository();
        var queue = new MemoryNotificationQueue();
        var service = new ContactService(repo, queue);

        var request = new CreateContactRequest("Jane", "jane@example.com", null, "Support", "Question");
        var created = await service.SubmitAsync(request);

        var updated = await service.UpdateStatusAsync(created.Id, ContactStatus.Resolved, "staff_admin_42");

        updated.ShouldNotBeNull();
        updated.Status.ShouldBe(ContactStatus.Resolved.ToString());
        updated.HandledBy.ShouldBe("staff_admin_42");
        updated.UpdatedAt.ShouldNotBeNull();

        var inRepo = repo.Items.Single();
        inRepo.Status.ShouldBe(ContactStatus.Resolved);
        inRepo.HandledBy.ShouldBe("staff_admin_42");
    }

    private sealed class MemoryContactRepository : IContactRepository
    {
        public List<ContactRequest> Items { get; } = [];

        public Task<ContactRequest?> GetAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(c => c.Id == id));

        public Task<List<ContactRequest>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult(Items.OrderByDescending(c => c.SubmittedAt).ToList());

        public Task<string> CreateAsync(ContactRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(request.Id)) request.Id = Guid.NewGuid().ToString("N");
            Items.Add(request);
            return Task.FromResult(request.Id);
        }

        public Task UpdateStatusAsync(string id, ContactStatus status, string handledBy, CancellationToken ct = default)
        {
            var item = Items.Find(c => c.Id == id);
            if (item is not null)
            {
                item.Status = status;
                item.HandledBy = handledBy;
                item.UpdatedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public Task UpdateNotificationStateAsync(
            string id, ContactNotificationStatus status, string? error, DateTime? sentAt, CancellationToken ct = default)
        {
            var item = Items.Find(c => c.Id == id);
            if (item is not null)
            {
                item.NotificationStatus = status;
                item.NotificationError = error;
                item.NotificationSentAt = sentAt;
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken ct = default)
        {
            Items.RemoveAll(c => c.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryNotificationQueue : IContactNotificationQueue
    {
        public List<ContactNotificationJob> EnqueuedJobs { get; } = [];

        public ValueTask EnqueueAsync(ContactNotificationJob job, CancellationToken ct = default)
        {
            EnqueuedJobs.Add(job);
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<ContactNotificationJob> ReadAllAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            foreach (var job in EnqueuedJobs)
            {
                yield return job;
            }
            await Task.Yield();
        }
    }

    private sealed class FailingNotificationQueue : IContactNotificationQueue
    {
        public ValueTask EnqueueAsync(ContactNotificationJob job, CancellationToken ct = default) =>
            throw new InvalidOperationException("Queue memory buffer exhausted");

        public async IAsyncEnumerable<ContactNotificationJob> ReadAllAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.Yield();
            yield break;
        }
    }
}
