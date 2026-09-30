namespace SimmerAndSear.Application.Abstractions;

// ── References ──────────────────────────────────────────────────────────────
//PayFast, n.d. Custom integration [Online guide].
//Available at: <https://developers.payfast.co.za/docs#custom_integration> [Accessed 25 September 2026].
//
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 25 September 2026].
// ────────────────────────────────────────────────────────────────────────────

public sealed record PaymentRequest(
    string OrderId,
    string OrderReference,
    decimal Amount,
    string CustomerName,
    string CustomerEmail,
    string Description);


public sealed record PaymentRedirect(
    string PostUrl,
    IReadOnlyList<KeyValuePair<string, string>> Fields);

public enum NotificationRejection
{
    None,
    BadSignature,
    UnknownSource,
    GatewayDeniedIt,
    Malformed
}

public sealed record GatewayNotification(
    bool IsTrustworthy,
    NotificationRejection Rejection,
    string? OrderId = null,
    string? GatewayReference = null,
    decimal Amount = 0m,
    string? PaymentStatus = null)
{
    public bool IsComplete =>
        string.Equals(PaymentStatus, "COMPLETE", StringComparison.OrdinalIgnoreCase);

    public static GatewayNotification Rejected(NotificationRejection reason) => new(false, reason);
}

/// <summary>A hosted payment gateway the customer is redirected to.</summary>
public interface IPaymentGateway
{
    /// <summary>Recorded on the order so a payment can be traced back to its provider.</summary>
    string Provider { get; }


    bool IsConfigured { get; }

    PaymentRedirect BuildRedirect(
        PaymentRequest request, string returnPath, string cancelPath, string notifyPath);
    Task<GatewayNotification> VerifyNotificationAsync(
        string rawBody, string? remoteIp, CancellationToken ct = default);
}
