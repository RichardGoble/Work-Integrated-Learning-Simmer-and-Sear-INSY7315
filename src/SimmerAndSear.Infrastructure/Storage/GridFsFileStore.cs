using MongoDB.Bson;
using MongoDB.Driver.GridFS;
using SimmerAndSear.Application.Abstractions;

namespace SimmerAndSear.Infrastructure.Storage;

/// <summary>
/// Binary storage over GridFS. Document records and their bytes now live in the
/// same database, which keeps the two writes recoverable without a distributed
/// transaction.
/// </summary>
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

    /// <summary>
    /// Returns an open read stream.
    /// <para>
    /// Callers must hand this straight to the response. Reading it into a
    /// MemoryStream first would put the whole file in the heap of a 1.75 GB
    /// instance, and a dozen concurrent product images would be enough to matter.
    /// </para>
    /// </summary>
    public async Task<Stream> OpenDownloadAsync(string fileId, CancellationToken ct = default) =>
        await bucket.OpenDownloadStreamAsync(ObjectId.Parse(fileId), cancellationToken: ct);

    public async Task DeleteAsync(string fileId, CancellationToken ct = default) =>
        await bucket.DeleteAsync(ObjectId.Parse(fileId), ct);
}
