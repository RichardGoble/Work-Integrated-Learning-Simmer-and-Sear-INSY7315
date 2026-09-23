using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Engagement;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Infrastructure.Engagement;

namespace SimmerAndSear.UnitTests.Engagement;

public sealed class BrevoNotificationWorkerTests
{
    [Fact]
    public async Task Worker_reads_dynamic_sales_notification_email_and_marks_sent()
    {
        var queue = new ContactNotificationQueue();
        var repo = new MemoryContactRepo();
        var settingsProvider = new MutableSettingsProvider("sales-initial@simmerandsear.co.za");
        var emailSender = new MemoryEmailSender { ShouldSucceed = true };

        var contact = new ContactRequest
        {
            Id = "contact_1",
            FullName = "Alice",
            Email = "alice@example.com",
            InquiryType = "Order Question",
            Message = "Need assistance",
            NotificationStatus = ContactNotificationStatus.Pending
        };
        repo.Items.Add(contact);

        var services = new ServiceCollection();
        services.AddSingleton<IContactRepository>(repo);
        services.AddSingleton<ISiteSettingsProvider>(settingsProvider);
        services.AddSingleton<ITransactionalEmailSender>(emailSender);
        var provider = services.BuildServiceProvider();

        var worker = new ContactNotificationWorker(queue, provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<ContactNotificationWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var workerTask = worker.StartAsync(cts.Token);

        // Dynamically update SalesNotificationEmail
        settingsProvider.CurrentEmail = "sales-dynamic@simmerandsear.co.za";

        await queue.EnqueueAsync(new ContactNotificationJob(
            contact.Id, contact.FullName, contact.Email, null, contact.InquiryType, contact.Message, contact.SubmittedAt));

        // Wait a brief moment for worker to process
        await Task.Delay(100);

        contact.NotificationStatus.ShouldBe(ContactNotificationStatus.Sent);
        contact.NotificationSentAt.ShouldNotBeNull();
        emailSender.SentCalls.Count.ShouldBe(1);
        emailSender.SentCalls[0].RecipientEmail.ShouldBe("sales-dynamic@simmerandsear.co.za");

        await worker.StopAsync(CancellationToken.None);
        cts.Cancel();
    }

    [Fact]
    public async Task Worker_survives_failed_send_and_marks_failed()
    {
        var queue = new ContactNotificationQueue();
        var repo = new MemoryContactRepo();
        var settingsProvider = new MutableSettingsProvider("sales@simmerandsear.co.za");
        var emailSender = new MemoryEmailSender { ShouldSucceed = false };

        var contact1 = new ContactRequest { Id = "contact_fail", NotificationStatus = ContactNotificationStatus.Pending };
        var contact2 = new ContactRequest { Id = "contact_success", NotificationStatus = ContactNotificationStatus.Pending };
        repo.Items.Add(contact1);
        repo.Items.Add(contact2);

        var services = new ServiceCollection();
        services.AddSingleton<IContactRepository>(repo);
        services.AddSingleton<ISiteSettingsProvider>(settingsProvider);
        services.AddSingleton<ITransactionalEmailSender>(emailSender);
        var provider = services.BuildServiceProvider();

        var worker = new ContactNotificationWorker(queue, provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<ContactNotificationWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await worker.StartAsync(cts.Token);

        // First message fails
        await queue.EnqueueAsync(new ContactNotificationJob(
            contact1.Id, "Fail User", "fail@example.com", null, "Inquiry", "Msg", DateTime.UtcNow));

        await Task.Delay(50);
        contact1.NotificationStatus.ShouldBe(ContactNotificationStatus.Failed);

        // Worker survives and can send second message when sender recovers
        emailSender.ShouldSucceed = true;
        await queue.EnqueueAsync(new ContactNotificationJob(
            contact2.Id, "Success User", "ok@example.com", null, "Inquiry", "Msg", DateTime.UtcNow));

        await Task.Delay(50);
        contact2.NotificationStatus.ShouldBe(ContactNotificationStatus.Sent);

        await worker.StopAsync(CancellationToken.None);
        cts.Cancel();
    }

    [Fact]
    public void BrevoOptions_contains_no_hardcoded_keys()
    {
        var options = new BrevoOptions();
        options.ApiKey.ShouldBeEmpty();
        options.SenderEmail.ShouldBeEmpty();
        options.SenderName.ShouldBe("Simmer and Sear");
    }

    private sealed class MutableSettingsProvider(string initialEmail) : ISiteSettingsProvider
    {
        public string CurrentEmail { get; set; } = initialEmail;

        public Task<SiteSettings> CurrentAsync(CancellationToken ct = default) =>
            Task.FromResult(new SiteSettings { SalesNotificationEmail = CurrentEmail });

        public void Invalidate() { }
    }

    private sealed class MemoryEmailSender : ITransactionalEmailSender
    {
        public bool ShouldSucceed { get; set; } = true;
        public List<(string RecipientEmail, string Subject)> SentCalls { get; } = [];

        public Task<bool> SendEmailAsync(
            string recipientEmail, string recipientName, string subject, string textContent, CancellationToken ct = default)
        {
            SentCalls.Add((recipientEmail, subject));
            return Task.FromResult(ShouldSucceed);
        }
    }

    private sealed class MemoryContactRepo : IContactRepository
    {
        public List<ContactRequest> Items { get; } = [];

        public Task<ContactRequest?> GetAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(c => c.Id == id));

        public Task<List<ContactRequest>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult(Items);

        public Task<string> CreateAsync(ContactRequest request, CancellationToken ct = default)
        {
            Items.Add(request);
            return Task.FromResult(request.Id);
        }

        public Task UpdateStatusAsync(string id, ContactStatus status, string handledBy, CancellationToken ct = default) =>
            Task.CompletedTask;

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

        public Task DeleteAsync(string id, CancellationToken ct = default) => Task.CompletedTask;
    }
}
