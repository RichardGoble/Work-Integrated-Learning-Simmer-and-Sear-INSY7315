using SimmerAndSear.Contracts.Wishlist;

namespace SimmerAndSear.Web.ViewModels;


public sealed record WishlistPageVm(
    WishlistResponse Wishlist,
    decimal BulkOrderThreshold,
    string Currency)
{
    public decimal Total => Wishlist.Items.Sum(i => i.UnitPrice);

    public bool ReachesThreshold => BulkOrderThreshold > 0 && Total >= BulkOrderThreshold;
}
