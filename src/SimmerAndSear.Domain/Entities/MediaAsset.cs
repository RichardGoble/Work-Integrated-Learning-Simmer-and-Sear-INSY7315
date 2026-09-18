using SimmerAndSear.Domain.Abstractions;

namespace SimmerAndSear.Domain.Entities;
public sealed class MediaAsset : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    public string GridFsId { get; set; } = string.Empty;
    public string? ThumbnailGridFsId { get; set; }

    public int? Width { get; set; }
    public int? Height { get; set; }

    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
