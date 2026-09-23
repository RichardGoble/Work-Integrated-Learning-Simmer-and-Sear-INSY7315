using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Engagement;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Infrastructure.Engagement;

public sealed class ContactNotificationWorker(
    IContactNotificationQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<ContactNotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ContactNotificationWorker background service started.");

        await foreach (var job in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessNotificationAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error processing contact notification for {ContactRequestId}", job.ContactRequestId);
            }
        }

        logger.LogInformation("ContactNotificationWorker background service stopping.");
    }

    private async Task ProcessNotificationAsync(ContactNotificationJob job, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var contactRepo = scope.ServiceProvider.GetRequiredService<IContactRepository>();
        var settingsProvider = scope.ServiceProvider.GetRequiredService<ISiteSettingsProvider>();
        var emailSender = scope.ServiceProvider.GetRequiredService<ITransactionalEmailSender>();

        // Dynamically resolve recipient from site settings AT SEND TIME
        var settings = await settingsProvider.CurrentAsync(ct);
        var recipientEmail = settings.SalesNotificationEmail?.Trim();

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            logger.LogWarning(
                "SalesNotificationEmail is not configured. Contact notification for {ContactRequestId} marked as Failed.",
                job.ContactRequestId);

            await contactRepo.UpdateNotificationStateAsync(
                job.ContactRequestId,
                ContactNotificationStatus.Failed,
                "SalesNotificationEmail is not configured in site settings.",
                null,
                ct);
            return;
        }

        var subject = $"[Simmer & Sear] New Contact Inquiry: {job.InquiryType}";
        var textContent =
            $"You have received a new contact inquiry on Simmer & Sear.\n\n" +
            $"Name: {job.FullName}\n" +
            $"Email: {job.Email}\n" +
            $"Phone: {job.Phone ?? "Not provided"}\n" +
            $"Inquiry Type: {job.InquiryType}\n" +
            $"Submitted At (UTC): {job.SubmittedAt:yyyy-MM-dd HH:mm:ss}\n\n" +
            $"Message:\n{job.Message}\n";

        var sent = await emailSender.SendEmailAsync(
            recipientEmail,
            "Simmer & Sear Sales",
            subject,
            textContent,
            ct);

        if (sent)
        {
            await contactRepo.UpdateNotificationStateAsync(
                job.ContactRequestId,
                ContactNotificationStatus.Sent,
                null,
                DateTime.UtcNow,
                ct);
        }
        else
        {
            await contactRepo.UpdateNotificationStateAsync(
                job.ContactRequestId,
                ContactNotificationStatus.Failed,
                "Email delivery provider was unavailable or rejected the request.",
                null,
                ct);
        }
    }
}
