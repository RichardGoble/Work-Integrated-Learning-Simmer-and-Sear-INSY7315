namespace SimmerAndSear.Infrastructure.Payments;

// ── References ──────────────────────────────────────────────────────────────
//PayFast, n.d. Sandbox [Online guide].
//Available at: <https://developers.payfast.co.za/docs#testing> [Accessed 25 September 2026].
//
//Microsoft, 2026. Options pattern [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/core/extensions/options> [Accessed 25 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class PayFastOptions
{
    public const string SectionName = "PayFast";

    public const string ProviderName = "PAYFAST";

    public bool Enabled { get; set; }

    public bool UseSandbox { get; set; } = true;

    public string MerchantId { get; set; } = string.Empty;

    public string MerchantKey { get; set; } = string.Empty;

    public string Passphrase { get; set; } = string.Empty;

    public string PublicBaseUrl { get; set; } = string.Empty;

    public string ProcessUrl =>
        UseSandbox
            ? "https://sandbox.payfast.co.za/eng/process"
            : "https://www.payfast.co.za/eng/process";

    public string ValidateUrl =>
        UseSandbox
            ? "https://sandbox.payfast.co.za/eng/query/validate"
            : "https://www.payfast.co.za/eng/query/validate";

    public static readonly string[] NotificationHosts =
    [
        "www.payfast.co.za",
        "sandbox.payfast.co.za",
        "w1w.payfast.co.za",
        "w2w.payfast.co.za"
    ];

    public bool IsConfigured =>
        Enabled &&
        !string.IsNullOrWhiteSpace(MerchantId) &&
        !string.IsNullOrWhiteSpace(MerchantKey) &&
        !string.IsNullOrWhiteSpace(PublicBaseUrl);
}
