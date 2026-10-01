using SimmerAndSear.Domain.Abstractions;

namespace SimmerAndSear.Domain.Entities;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. C# classes [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/classes> [Accessed 18 September 2026].
//
//Microsoft, 2025. Properties [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────

public sealed class GalleryImage : IEntity
{
    public const int SlotCount = 4;

    public string Id { get; set; } = string.Empty;


    public int Slot { get; set; }

    public string MediaAssetId { get; set; } = string.Empty;


    public string Caption { get; set; } = string.Empty;

    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public static bool IsValidSlot(int slot) => slot is >= 1 and <= SlotCount;
}
