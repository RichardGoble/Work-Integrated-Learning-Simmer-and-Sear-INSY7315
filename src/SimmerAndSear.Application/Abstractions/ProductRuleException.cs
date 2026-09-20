namespace SimmerAndSear.Application.Abstractions;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2022. How to: Create User-Defined Exceptions [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/standard/exceptions/how-to-create-user-defined-exceptions> [Accessed 20 September 2026].



public sealed class ProductRuleException(string message) : Exception(message);
