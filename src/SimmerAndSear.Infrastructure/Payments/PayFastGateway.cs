using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Infrastructure.Payments;

// ── References ──────────────────────────────────────────────────────────────
//PayFast, n.d. Instant Transaction Notification [Online guide].
//Available at: <https://developers.payfast.co.za/docs#instant_transaction_notification> [Accessed 25 September 2026].
//
//PayFast, n.d. Custom integration [Online guide].
//Available at: <https://developers.payfast.co.za/docs#custom_integration> [Accessed 25 September 2026].
//
//Microsoft, 2026. Make HTTP requests using IHttpClientFactory [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0> [Accessed 25 September 2026].
// ────────────────────────────────────────────────────────────────────────────

public sealed class PayFastGateway(
    IOptions<PayFastOptions> options,
    IHttpClientFactory httpClientFactory,
    ILogger<PayFastGateway> logger) : IPaymentGateway
{
    public const string HttpClientName = "payfast";

    private readonly PayFastOptions _options = options.Value;

    public string Provider => PayFastOptions.ProviderName;

    public bool IsConfigured => _options.IsConfigured;

    public PaymentRedirect BuildRedirect(
        PaymentRequest request, string returnPath, string cancelPath, string notifyPath)
    {
        var origin = _options.PublicBaseUrl.TrimEnd('/');

        var fields = new List<KeyValuePair<string, string>>
        {
            new("merchant_id", _options.MerchantId),
            new("merchant_key", _options.MerchantKey),
            new("return_url", $"{origin}{returnPath}"),
            new("cancel_url", $"{origin}{cancelPath}"),
            new("notify_url", $"{origin}{notifyPath}"),
            new("name_first", First(request.CustomerName)),
            new("name_last", Last(request.CustomerName)),
            new("email_address", request.CustomerEmail),
            new("m_payment_id", request.OrderId),
            new("amount", request.Amount.ToString("F2", CultureInfo.InvariantCulture)),
            new("item_name", $"Simmer & Sear order {request.OrderReference}"),
            new("item_description", request.Description)
        };

        fields.Add(new KeyValuePair<string, string>(
            "signature", PayFastSignature.Create(fields, _options.Passphrase)));

        return new PaymentRedirect(
            _options.ProcessUrl,
            [.. fields.Where(f => !string.IsNullOrWhiteSpace(f.Value))]);
    }

    public async Task<GatewayNotification> VerifyNotificationAsync(
        string rawBody, string? remoteIp, CancellationToken ct = default)
    {
        if (!PayFastSignature.Verify(rawBody, _options.Passphrase, out _))
        {
            logger.LogWarning("PayFast notification refused: signature did not match.");
            return GatewayNotification.Rejected(NotificationRejection.BadSignature);
        }

        if (!await IsFromPayFastAsync(remoteIp))
        {
            logger.LogWarning("PayFast notification refused: unexpected source {RemoteIp}.", remoteIp);
            return GatewayNotification.Rejected(NotificationRejection.UnknownSource);
        }

        if (!await GatewayConfirmsAsync(rawBody, ct))
        {
            logger.LogWarning("PayFast notification refused: PayFast did not confirm it.");
            return GatewayNotification.Rejected(NotificationRejection.GatewayDeniedIt);
        }

        var values = PayFastSignature.ParseOrdered(rawBody)
            .ToDictionary(p => p.Key, p => PayFastSignature.Decode(p.Value));

        if (!values.TryGetValue("m_payment_id", out var orderId) ||
            string.IsNullOrWhiteSpace(orderId) ||
            !decimal.TryParse(Value(values, "amount_gross"),
                NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            logger.LogWarning("PayFast notification refused: required fields missing.");
            return GatewayNotification.Rejected(NotificationRejection.Malformed);
        }

        return new GatewayNotification(
            IsTrustworthy: true,
            Rejection: NotificationRejection.None,
            OrderId: orderId,
            GatewayReference: Value(values, "pf_payment_id"),
            Amount: amount,
            PaymentStatus: Value(values, "payment_status"));
    }

    private async Task<bool> IsFromPayFastAsync(string? remoteIp)
    {
        if (!IPAddress.TryParse(remoteIp, out var caller))
        {
            return false;
        }

        foreach (var host in PayFastOptions.NotificationHosts)
        {
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(host);

                if (addresses.Any(a => a.Equals(caller)))
                {
                    return true;
                }
            }
            catch (SocketException ex)
            {
                logger.LogWarning(ex, "Could not resolve {Host} while checking a notification.", host);
            }
        }

        return false;
    }

    private async Task<bool> GatewayConfirmsAsync(string rawBody, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);

            using var content = new StringContent(
                rawBody, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await client.PostAsync(_options.ValidateUrl, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            return body.Contains("VALID", StringComparison.OrdinalIgnoreCase) &&
                   !body.Contains("INVALID", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not reach PayFast to confirm a notification.");
            return false;
        }
    }

    private static string Value(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) ? value : string.Empty;

    private static string First(string? fullName)
    {
        var name = (fullName ?? string.Empty).Trim();
        var split = name.IndexOf(' ');
        return split < 0 ? name : name[..split];
    }

    private static string Last(string? fullName)
    {
        var name = (fullName ?? string.Empty).Trim();
        var split = name.LastIndexOf(' ');
        return split < 0 ? string.Empty : name[(split + 1)..];
    }

}
