using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 18 September 2026].
//
//MongoDB, n.d. CRUD Operations [Source code].
//Available at: <https://www.mongodb.com/docs/drivers/csharp/current/crud/> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public interface IGalleryRepository
{
    Task<List<GalleryImage>> ListAsync(CancellationToken ct = default);

    Task<GalleryImage?> GetBySlotAsync(int slot, CancellationToken ct = default);


    Task SetAsync(GalleryImage image, CancellationToken ct = default);

    Task ClearAsync(int slot, CancellationToken ct = default);
}
