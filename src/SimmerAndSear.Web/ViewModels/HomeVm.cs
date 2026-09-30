using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Web.ViewModels.Components;

namespace SimmerAndSear.Web.ViewModels;


public sealed record HomeVm(List<CardVm> Featured, List<GalleryImage> Gallery)
{
    public GalleryImage? Slot(int slot) => Gallery.FirstOrDefault(g => g.Slot == slot);

    public bool HasGallery => Gallery.Count > 0;
}
