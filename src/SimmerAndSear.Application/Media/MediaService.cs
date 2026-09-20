using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Media;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Dependency injection in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0> [Accessed 20 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 20 September 2026].
//
//Microsoft, 2026. C# classes [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/classes> [Accessed 20 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class MediaService(
    IMediaAssetRepository mediaRepository,
    IFileStore fileStore,
    IImageProcessor imageProcessor) : IMediaService
{
    private const long MaxUploadSizeBytes = 10 * 1024 * 1024; // 10 MB

    public async Task<MediaAsset> UploadAsync(
        Stream stream,
        string rawFileName,
        long fileSizeBytes,
        string? uploadedBy,
        CancellationToken ct = default)
    {
        if (stream == null || fileSizeBytes <= 0)
        {
            throw new MediaValidationException("File is empty or missing.");
        }

        if (fileSizeBytes > MaxUploadSizeBytes)
        {
            throw new MediaValidationException($"File exceeds maximum allowed size of {MaxUploadSizeBytes / (1024 * 1024)} MB.");
        }

        // Buffer into MemoryStream if input stream is not seekable so we can validate, upload, and resize
        MemoryStream? memoryBuffer = null;
        Stream seekableStream;
        if (stream.CanSeek)
        {
            seekableStream = stream;
        }
        else
        {
            memoryBuffer = new MemoryStream();
            await stream.CopyToAsync(memoryBuffer, ct);
            if (memoryBuffer.Length == 0)
            {
                throw new MediaValidationException("File is empty or missing.");
            }
            if (memoryBuffer.Length > MaxUploadSizeBytes)
            {
                throw new MediaValidationException($"File exceeds maximum allowed size of {MaxUploadSizeBytes / (1024 * 1024)} MB.");
            }
            seekableStream = memoryBuffer;
        }

        try
        {
            seekableStream.Position = 0;
            var validation = await imageProcessor.ValidateImageAsync(seekableStream, ct);
            if (!validation.IsValid)
            {
                throw new MediaValidationException(validation.ErrorMessage ?? "Unsupported or corrupt image.");
            }

            var safeFileName = Path.GetFileName(rawFileName);
            if (string.IsNullOrWhiteSpace(safeFileName))
            {
                safeFileName = $"upload_{Guid.NewGuid():N}";
            }

            string? originalGridFsId = null;
            string? thumbGridFsId = null;

            try
            {
                // 1. Upload original image
                seekableStream.Position = 0;
                originalGridFsId = await fileStore.UploadAsync(
                    seekableStream, safeFileName, validation.ContentType, ct);

                // 2. Generate thumbnail stream
                seekableStream.Position = 0;
                await using var thumbStream = await imageProcessor.CreateThumbnailAsync(
                    seekableStream, maxDimension: 400, ct);

                // 3. Upload thumbnail image
                thumbGridFsId = await fileStore.UploadAsync(
                    thumbStream, $"thumb_{safeFileName}", validation.ContentType, ct);

                // 4. Persist MediaAsset entity
                var asset = new MediaAsset
                {
                    FileName = safeFileName,
                    ContentType = validation.ContentType,
                    FileSizeBytes = fileSizeBytes,
                    GridFsId = originalGridFsId,
                    ThumbnailGridFsId = thumbGridFsId,
                    Width = validation.Width,
                    Height = validation.Height,
                    UploadedBy = uploadedBy ?? string.Empty,
                    UploadedAt = DateTime.UtcNow
                };

                await mediaRepository.CreateAsync(asset, ct);
                return asset;
            }
            catch
            {
                // Compensating cleanup: do not leave orphaned GridFS files
                if (thumbGridFsId != null)
                {
                    try { await fileStore.DeleteAsync(thumbGridFsId, CancellationToken.None); }
                    catch { /* preserve primary exception */ }
                }

                if (originalGridFsId != null)
                {
                    try { await fileStore.DeleteAsync(originalGridFsId, CancellationToken.None); }
                    catch { /* preserve primary exception */ }
                }

                throw;
            }
        }
        finally
        {
            if (memoryBuffer != null)
            {
                await memoryBuffer.DisposeAsync();
            }
        }
    }

    public async Task<MediaStreamResult?> GetOriginalStreamAsync(string id, CancellationToken ct = default)
    {
        var asset = await mediaRepository.GetAsync(id, ct);
        if (asset is null || string.IsNullOrWhiteSpace(asset.GridFsId))
        {
            return null;
        }

        var stream = await fileStore.OpenDownloadAsync(asset.GridFsId, ct);
        var etag = $"\"{asset.GridFsId}\"";
        return new MediaStreamResult(stream, asset.ContentType, etag, asset.FileSizeBytes);
    }

    public async Task<MediaStreamResult?> GetThumbnailStreamAsync(string id, CancellationToken ct = default)
    {
        var asset = await mediaRepository.GetAsync(id, ct);
        if (asset is null || string.IsNullOrWhiteSpace(asset.ThumbnailGridFsId))
        {
            return null;
        }

        var stream = await fileStore.OpenDownloadAsync(asset.ThumbnailGridFsId, ct);
        var etag = $"\"{asset.ThumbnailGridFsId}\"";
        return new MediaStreamResult(stream, asset.ContentType, etag);
    }
}
