namespace SimmerAndSear.Application.Engagement;

public interface ITransactionalEmailSender
{
    Task<bool> SendEmailAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string textContent,
        CancellationToken ct = default);
}
