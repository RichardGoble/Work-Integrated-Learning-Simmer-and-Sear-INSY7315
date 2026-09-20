namespace SimmerAndSear.Application.Abstractions;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2022. How to: Create User-Defined Exceptions [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/standard/exceptions/how-to-create-user-defined-exceptions> [Accessed 20 September 2026].
//
//Microsoft, 2026. C# classes [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/classes> [Accessed 20 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class MediaValidationException(string message) : Exception(message);
