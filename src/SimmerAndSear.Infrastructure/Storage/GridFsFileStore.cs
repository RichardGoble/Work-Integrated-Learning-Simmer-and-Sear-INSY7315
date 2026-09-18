using MongoDB.Bson;
using MongoDB.Driver.GridFS;
using SimmerAndSear.Application.Abstractions;

namespace SimmerAndSear.Infrastructure.Storage;
// ── References ──────────────────────────────────────────────────────────────
//MongoDB, n.d. Store Large Files [Source code].
//Available at: <https://www.mongodb.com/docs/drivers/csharp/current/crud/gridfs/> [Accessed 18 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class GridFsFileStore(IGridFSBucket bucket) : IFileStore
{
    public async Task<string> UploadAsync(
        Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var options = new GridFSUploadOptions
        {
            Metadata = new BsonDocument { { "contentType", contentType } }
        };

        var id = await bucket.UploadFromStreamAsync(fileName, content, options, ct);
        return id.ToString();
    }
    public async Task<Stream> OpenDownloadAsync(string fileId, CancellationToken ct = default) =>
        await bucket.OpenDownloadStreamAsync(ObjectId.Parse(fileId), cancellationToken: ct);

    public async Task DeleteAsync(string fileId, CancellationToken ct = default) =>
        await bucket.DeleteAsync(ObjectId.Parse(fileId), ct);
}
