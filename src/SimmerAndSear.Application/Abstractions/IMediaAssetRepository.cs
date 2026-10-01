using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 18 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public interface IMediaAssetRepository
{
    Task<MediaAsset?> GetAsync(string id, CancellationToken ct = default);

    Task<string> CreateAsync(MediaAsset asset, CancellationToken ct = default);

    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<MediaAsset>> ListAsync(int limit = 50, CancellationToken ct = default);
    Task<IReadOnlyList<MediaAsset>> FindOrphansAsync(DateTime olderThanUtc, CancellationToken ct = default);
}
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 18 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public interface IFileStore
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
    Task<Stream> OpenDownloadAsync(string fileId, CancellationToken ct = default);

    Task DeleteAsync(string fileId, CancellationToken ct = default);
}
