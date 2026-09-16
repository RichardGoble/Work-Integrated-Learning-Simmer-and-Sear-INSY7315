using SimmerAndSear.Domain.Abstractions;

namespace SimmerAndSear.Domain.Entities;

/// <summary>
/// Metadata for a binary held in GridFS. The bytes live in the file store; this
/// record is what everything else references.
/// </summary>
public sealed class MediaAsset : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    public string GridFsId { get; set; } = string.Empty;

    /// <summary>
    /// Generated on upload. Without it a twelve-product grid streams twelve
    /// full-size images through App Service on every page load.
    /// </summary>
    public string? ThumbnailGridFsId { get; set; }

    public int? Width { get; set; }
    public int? Height { get; set; }

    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
