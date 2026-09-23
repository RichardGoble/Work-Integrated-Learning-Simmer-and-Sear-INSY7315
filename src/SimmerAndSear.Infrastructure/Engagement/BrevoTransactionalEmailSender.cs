using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimmerAndSear.Application.Engagement;

namespace SimmerAndSear.Infrastructure.Engagement;

public sealed class BrevoTransactionalEmailSender(
    HttpClient httpClient,
    IOptions<BrevoOptions> options,
    ILogger<BrevoTransactionalEmailSender> logger) : ITransactionalEmailSender
{
    private const string BrevoEndpoint = "https://api.brevo.com/v3/smtp/email";

    public async Task<bool> SendEmailAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string textContent,
        CancellationToken ct = default)
    {
        var opts = options.Value;

        if (string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            logger.LogWarning("Brevo API key is not configured. Email to {RecipientEmail} skipped.", recipientEmail);
            return false;
        }

        if (string.IsNullOrWhiteSpace(opts.SenderEmail))
        {
            logger.LogWarning("Brevo SenderEmail is not configured. Email to {RecipientEmail} skipped.", recipientEmail);
            return false;
        }

        try
        {
            var payload = new
            {
                sender = new { name = opts.SenderName, email = opts.SenderEmail },
                to = new[] { new { name = recipientName, email = recipientEmail } },
                subject,
                textContent
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, BrevoEndpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("api-key", opts.ApiKey);

            using var response = await httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Successfully sent transactional notification email to {RecipientEmail}", recipientEmail);
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning(
                "Brevo API rejected email to {RecipientEmail}. StatusCode: {StatusCode}, Details: {Details}",
                recipientEmail,
                (int)response.StatusCode,
                errorBody.Length > 200 ? errorBody[..200] : errorBody);

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Brevo network error while delivering email to {RecipientEmail}", recipientEmail);
            return false;
        }
    }
}
