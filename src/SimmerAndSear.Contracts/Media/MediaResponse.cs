namespace SimmerAndSear.Contracts.Media;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. C# record types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records> [Accessed 20 September 2026].
//
//Microsoft, 2025. Properties [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties> [Accessed 20 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed record MediaResponse(
    string Id,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    int Width,
    int Height,
    string OriginalUrl,
    string ThumbnailUrl,
    DateTime UploadedAt);
