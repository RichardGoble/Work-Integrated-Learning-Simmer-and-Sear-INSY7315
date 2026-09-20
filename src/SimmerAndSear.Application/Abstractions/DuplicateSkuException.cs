namespace SimmerAndSear.Application.Abstractions;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2022. How to: Create User-Defined Exceptions [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/standard/exceptions/how-to-create-user-defined-exceptions> [Accessed 20 September 2026].
//MongoDB, n.d. Unique Indexes [Source code].
//Available at: <https://www.mongodb.com/docs/manual/core/index-unique/> [Accessed 20 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class DuplicateSkuException(string sku)
    : Exception($"A product with SKU '{sku}' already exists.")
{
    public string Sku { get; } = sku;
}
