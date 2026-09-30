using System.Security.Cryptography;
using System.Text;

namespace SimmerAndSear.Infrastructure.Payments;

// ── References ──────────────────────────────────────────────────────────────
//PayFast, n.d. Signature generation [Online guide].
//Available at: <https://developers.payfast.co.za/docs#signature> [Accessed 25 September 2026].
//
//PHP, n.d. urlencode [Online guide].
//Available at: <https://www.php.net/manual/en/function.urlencode.php> [Accessed 25 September 2026].
//
//Microsoft, 2025. Uri.EscapeDataString Method [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.uri.escapedatastring> [Accessed 25 September 2026].
// ────────────────────────────────────────────────────────────────────────────

public static class PayFastSignature
{

    public static string Create(
        IEnumerable<KeyValuePair<string, string>> fields, string? passphrase)
    {
        var payload = string.Join("&", fields
            .Where(f => !string.IsNullOrWhiteSpace(f.Value))
            .Select(f => $"{f.Key}={PhpUrlEncode(f.Value.Trim())}"));

        if (!string.IsNullOrWhiteSpace(passphrase))
        {
            payload += $"&passphrase={PhpUrlEncode(passphrase.Trim())}";
        }

        return Md5(payload);
    }

    public static bool Verify(string rawBody, string? passphrase, out string? receivedSignature)
    {
        receivedSignature = null;

        var pairs = ParseOrdered(rawBody);
        var signature = pairs.FirstOrDefault(p => p.Key == "signature");

        if (signature.Key is null || string.IsNullOrWhiteSpace(signature.Value))
        {
            return false;
        }

        receivedSignature = signature.Value;

        var payload = string.Join("&", pairs
            .Where(p => p.Key != "signature")
            .Select(p => $"{p.Key}={p.Value}"));

        if (!string.IsNullOrWhiteSpace(passphrase))
        {
            payload += $"&passphrase={PhpUrlEncode(passphrase.Trim())}";
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Md5(payload)),
            Encoding.UTF8.GetBytes(receivedSignature.ToLowerInvariant()));
    }

    public static List<KeyValuePair<string, string>> ParseOrdered(string rawBody)
    {
        var pairs = new List<KeyValuePair<string, string>>();

        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return pairs;
        }

        foreach (var part in rawBody.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var split = part.IndexOf('=');

            pairs.Add(split < 0
                ? new KeyValuePair<string, string>(part, string.Empty)
                : new KeyValuePair<string, string>(part[..split], part[(split + 1)..]));
        }

        return pairs;
    }

    public static string Decode(string value) => Uri.UnescapeDataString(value.Replace("+", " "));

    private static string PhpUrlEncode(string value) =>
        Uri.EscapeDataString(value).Replace("%20", "+").Replace("~", "%7E");

    private static string Md5(string value) =>
        Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
