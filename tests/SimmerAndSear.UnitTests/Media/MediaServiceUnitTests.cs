using Shouldly;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Media;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Infrastructure.Media;

namespace SimmerAndSear.UnitTests.Media;

// ── References ──────────────────────────────────────────────────────────────
//xUnit.net, 2025. Getting Started with xUnit.net v2 [Source code].
//Available at: <https://xunit.net/docs/getting-started/v2/getting-started> [Accessed 20 September 2026].
//
//Shouldly, n.d. Shouldly Documentation [Source code].
//Available at: <https://docs.shouldly.org/> [Accessed 20 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class MediaServiceUnitTests
{
    private readonly MemoryMediaRepository _repo = new();
    private readonly MemoryFileStore _fileStore = new();
    private readonly ImageSharpImageProcessor _processor = new();

    private MediaService CreateService() => new(_repo, _fileStore, _processor);

    private static MemoryStream CreateValidImage(int width, int height, string format = "jpeg")
    {
        using var image = new Image<Rgba32>(width, height);
        var ms = new MemoryStream();
        switch (format.ToLowerInvariant())
        {
            case "png":
                image.Save(ms, new PngEncoder());
                break;
            case "webp":
                image.Save(ms, new WebpEncoder());
                break;
            default:
                image.Save(ms, new JpegEncoder());
                break;
        }
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public async Task Upload_valid_jpeg_stores_original_and_thumbnail_with_dimensions()
    {
        var service = CreateService();
        await using var stream = CreateValidImage(800, 600, "jpeg");
        var length = stream.Length;

        var asset = await service.UploadAsync(stream, "photo.jpg", length, "user-123");

        asset.ShouldNotBeNull();
        asset.FileName.ShouldBe("photo.jpg");
        asset.ContentType.ShouldBe("image/jpeg");
        asset.FileSizeBytes.ShouldBe(length);
        asset.Width.ShouldBe(800);
        asset.Height.ShouldBe(600);
        asset.UploadedBy.ShouldBe("user-123");
        asset.GridFsId.ShouldNotBeNullOrWhiteSpace();
        asset.ThumbnailGridFsId.ShouldNotBeNullOrWhiteSpace();
        asset.GridFsId.ShouldNotBe(asset.ThumbnailGridFsId);

        _fileStore.StoredFiles.ContainsKey(asset.GridFsId).ShouldBeTrue();
        _fileStore.StoredFiles.ContainsKey(asset.ThumbnailGridFsId!).ShouldBeTrue();
        _repo.Assets.ContainsKey(asset.Id).ShouldBeTrue();
    }

    [Fact]
    public async Task Upload_valid_png_and_webp_succeed()
    {
        var service = CreateService();

        await using var pngStream = CreateValidImage(400, 300, "png");
        var pngAsset = await service.UploadAsync(pngStream, "image.png", pngStream.Length, "user-1");
        pngAsset.ContentType.ShouldBe("image/png");
        pngAsset.Width.ShouldBe(400);
        pngAsset.Height.ShouldBe(300);

        await using var webpStream = CreateValidImage(500, 500, "webp");
        var webpAsset = await service.UploadAsync(webpStream, "image.webp", webpStream.Length, "user-2");
        webpAsset.ContentType.ShouldBe("image/webp");
        webpAsset.Width.ShouldBe(500);
        webpAsset.Height.ShouldBe(500);
    }

    [Fact]
    public async Task Upload_empty_stream_throws_validation_exception()
    {
        var service = CreateService();
        using var emptyStream = new MemoryStream();

        var ex = await Should.ThrowAsync<MediaValidationException>(async () =>
            await service.UploadAsync(emptyStream, "empty.jpg", 0, "user-1"));

        ex.Message.ShouldContain("empty");
    }

    [Fact]
    public async Task Upload_oversized_file_throws_validation_exception()
    {
        var service = CreateService();
        using var stream = new MemoryStream(new byte[10]);
        var oversizedLength = 11L * 1024 * 1024; // 11 MB

        var ex = await Should.ThrowAsync<MediaValidationException>(async () =>
            await service.UploadAsync(stream, "big.jpg", oversizedLength, "user-1"));

        ex.Message.ShouldContain("exceeds");
    }

    [Fact]
    public async Task Fake_image_with_jpg_extension_is_rejected()
    {
        var service = CreateService();
        var fakeBytes = System.Text.Encoding.UTF8.GetBytes("<html><script>alert('pwn')</script></html>");
        using var fakeStream = new MemoryStream(fakeBytes);

        var ex = await Should.ThrowAsync<MediaValidationException>(async () =>
            await service.UploadAsync(fakeStream, "innocent.jpg", fakeBytes.Length, "user-1"));

        ex.Message.ShouldContain("not a valid");
        _fileStore.StoredFiles.ShouldBeEmpty();
    }

    [Fact]
    public async Task Corrupt_image_data_is_rejected()
    {
        var service = CreateService();
        var corruptBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 }; // truncated JPEG header
        using var corruptStream = new MemoryStream(corruptBytes);

        var ex = await Should.ThrowAsync<MediaValidationException>(async () =>
            await service.UploadAsync(corruptStream, "corrupt.jpg", corruptBytes.Length, "user-1"));

        ex.Message.ShouldContain("corrupt");
        _fileStore.StoredFiles.ShouldBeEmpty();
    }

    [Fact]
    public async Task Thumbnail_preserves_aspect_ratio_and_bounds_within_400px()
    {
        await using var wideImageStream = CreateValidImage(1200, 600, "jpeg");
        await using var thumbStream = await _processor.CreateThumbnailAsync(wideImageStream, maxDimension: 400);

        var thumbInfo = await Image.IdentifyAsync(thumbStream);
        thumbInfo.ShouldNotBeNull();
        thumbInfo.Width.ShouldBe(400);
        thumbInfo.Height.ShouldBe(200);
    }

    [Fact]
    public async Task Compensating_cleanup_deletes_original_when_thumbnail_upload_fails()
    {
        _fileStore.FailOnUploadCount = 2; // Fail on 2nd upload (thumbnail)
        var service = CreateService();
        await using var stream = CreateValidImage(500, 500, "jpeg");

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await service.UploadAsync(stream, "test.jpg", stream.Length, "user-1"));

        // The original upload was stored, but then cleaned up when the thumbnail failed!
        _fileStore.StoredFiles.ShouldBeEmpty();
        _fileStore.DeletedFileIds.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Compensating_cleanup_deletes_both_when_database_insert_fails()
    {
        _repo.ThrowOnCreate = true;
        var service = CreateService();
        await using var stream = CreateValidImage(500, 500, "jpeg");

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await service.UploadAsync(stream, "test.jpg", stream.Length, "user-1"));

        // Both original and thumbnail were uploaded, then compensating cleanup deleted both!
        _fileStore.StoredFiles.ShouldBeEmpty();
        _fileStore.DeletedFileIds.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetOriginalStream_and_GetThumbnailStream_stream_directly_with_etag()
    {
        var service = CreateService();
        await using var stream = CreateValidImage(600, 400, "jpeg");
        var asset = await service.UploadAsync(stream, "kitchen.jpg", stream.Length, "admin");

        var original = await service.GetOriginalStreamAsync(asset.Id);
        original.ShouldNotBeNull();
        original.ContentType.ShouldBe("image/jpeg");
        original.ETag.ShouldBe($"\"{asset.GridFsId}\"");
        original.ContentStream.ShouldNotBeNull();

        var thumb = await service.GetThumbnailStreamAsync(asset.Id);
        thumb.ShouldNotBeNull();
        thumb.ContentType.ShouldBe("image/jpeg");
        thumb.ETag.ShouldBe($"\"{asset.ThumbnailGridFsId}\"");
        thumb.ContentStream.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetOriginalStream_returns_null_when_asset_is_missing()
    {
        var service = CreateService();
        var result = await service.GetOriginalStreamAsync("missing-id");
        result.ShouldBeNull();
    }
}

internal sealed class MemoryMediaRepository : IMediaAssetRepository
{
    public Dictionary<string, MediaAsset> Assets { get; } = new();
    public bool ThrowOnCreate { get; set; }

    public Task<MediaAsset?> GetAsync(string id, CancellationToken ct = default)
    {
        Assets.TryGetValue(id, out var asset);
        return Task.FromResult(asset);
    }

    public Task<string> CreateAsync(MediaAsset asset, CancellationToken ct = default)
    {
        if (ThrowOnCreate) throw new InvalidOperationException("DB insert error");
        if (string.IsNullOrWhiteSpace(asset.Id)) asset.Id = Guid.NewGuid().ToString("N");
        Assets[asset.Id] = asset;
        return Task.FromResult(asset.Id);
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        Assets.Remove(id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MediaAsset>> ListAsync(int limit = 50, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MediaAsset>>(Assets.Values.Take(limit).ToList());

    public Task<IReadOnlyList<MediaAsset>> FindOrphansAsync(DateTime olderThanUtc, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MediaAsset>>(Assets.Values.Where(a => a.UploadedAt < olderThanUtc).ToList());
}

internal sealed class MemoryFileStore : IFileStore
{
    public Dictionary<string, byte[]> StoredFiles { get; } = new();
    public List<string> DeletedFileIds { get; } = new();
    public int FailOnUploadCount { get; set; } = -1;
    private int _uploadCount;

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        _uploadCount++;
        if (_uploadCount == FailOnUploadCount)
        {
            throw new InvalidOperationException("FileStore upload simulated failure");
        }

        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        var id = Guid.NewGuid().ToString("N");
        StoredFiles[id] = ms.ToArray();
        return id;
    }

    public Task<Stream> OpenDownloadAsync(string fileId, CancellationToken ct = default)
    {
        if (!StoredFiles.TryGetValue(fileId, out var bytes))
        {
            throw new FileNotFoundException($"File {fileId} not found in store.");
        }
        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    public Task DeleteAsync(string fileId, CancellationToken ct = default)
    {
        StoredFiles.Remove(fileId);
        DeletedFileIds.Add(fileId);
        return Task.CompletedTask;
    }
}
