using SimmerAndSear.Domain.Abstractions;

namespace SimmerAndSear.Domain.Entities;
//References
//Microsoft, 2026. C# classes [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/classes> [Accessed 21 September 2026].

//MongoDB, n.d. GridFS [Source code].
//Available at: <https://www.mongodb.com/docs/drivers/csharp/current/crud/gridfs/> [Accessed 21 September 2026].

public sealed class QuotationDocument : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string OrderReference { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;

    public string GridFsId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    public decimal Total { get; set; }
    public string Currency { get; set; } = string.Empty;

    public DateTime ValidUntil { get; set; } = DateTime.UtcNow;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
