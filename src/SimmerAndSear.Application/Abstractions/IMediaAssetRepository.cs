using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;
public interface IMediaAssetRepository
{
    Task<MediaAsset?> GetAsync(string id, CancellationToken ct = default);

    Task<string> CreateAsync(MediaAsset asset, CancellationToken ct = default);

    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<MediaAsset>> FindOrphansAsync(DateTime olderThanUtc, CancellationToken ct = default);
}
public interface IFileStore
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
    Task<Stream> OpenDownloadAsync(string fileId, CancellationToken ct = default);

    Task DeleteAsync(string fileId, CancellationToken ct = default);
}
