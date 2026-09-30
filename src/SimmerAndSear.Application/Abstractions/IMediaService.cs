using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 20 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 20 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed record MediaStreamResult(
    Stream ContentStream,
    string ContentType,
    string ETag,
    long? FileSizeBytes = null);

public interface IMediaService
{
    Task<MediaAsset> UploadAsync(
        Stream stream,
        string rawFileName,
        long fileSizeBytes,
        string? uploadedBy,
        CancellationToken ct = default);

    Task<MediaStreamResult?> GetOriginalStreamAsync(string id, CancellationToken ct = default);

    Task<MediaStreamResult?> GetThumbnailStreamAsync(string id, CancellationToken ct = default);

    Task DeleteAsync(string id, CancellationToken ct = default);
}
