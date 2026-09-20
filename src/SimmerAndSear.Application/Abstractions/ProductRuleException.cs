namespace SimmerAndSear.Application.Abstractions;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2022. How to: Create User-Defined Exceptions [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/standard/exceptions/how-to-create-user-defined-exceptions> [Accessed 20 September 2026].
//
//MongoDB, n.d. Schema Validation [Source code].
//Available at: <https://www.mongodb.com/docs/manual/core/schema-validation/> [Accessed 20 September 2026].
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// A catalogue rule the caller broke, phrased so the message can be shown as-is.
/// </summary>
/// <remarks>
/// These rules are also enforced by the $jsonSchema validator on the collection, which is
/// the real guarantee. Checking here as well is what turns an opaque write failure into a
/// sentence an administrator can act on.
/// </remarks>
public sealed class ProductRuleException(string message) : Exception(message);
