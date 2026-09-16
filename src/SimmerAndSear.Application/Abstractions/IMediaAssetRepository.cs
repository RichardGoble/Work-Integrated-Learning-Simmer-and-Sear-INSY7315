using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;

/// <summary>
/// Metadata for binaries. The bytes themselves are handled by
/// <see cref="IFileStore"/>; splitting the two keeps the document store and the
/// file store independently replaceable.
/// </summary>
public interface IMediaAssetRepository
{
    Task<MediaAsset?> GetAsync(string id, CancellationToken ct = default);

    Task<string> CreateAsync(MediaAsset asset, CancellationToken ct = default);

    Task DeleteAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Assets no product, post or gallery entry references any more. Used by the
    /// orphan sweeper: if a file upload succeeds but its metadata write fails,
    /// the blob would otherwise leak.
    /// </summary>
    Task<IReadOnlyList<MediaAsset>> FindOrphansAsync(DateTime olderThanUtc, CancellationToken ct = default);
}

/// <summary>Streaming access to the binary store (GridFS).</summary>
public interface IFileStore
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Returns an open stream. Callers must stream it to the response and never
    /// buffer it into memory - a Basic-tier instance has 1.75 GB for everything.
    /// </summary>
    Task<Stream> OpenDownloadAsync(string fileId, CancellationToken ct = default);

    Task DeleteAsync(string fileId, CancellationToken ct = default);
}
