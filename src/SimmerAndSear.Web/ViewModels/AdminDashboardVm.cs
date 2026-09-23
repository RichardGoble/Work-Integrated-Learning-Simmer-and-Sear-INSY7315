using SimmerAndSear.Contracts.Admin;
using SimmerAndSear.Contracts.Catalogue;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Web.ViewModels;

public sealed class AdminDashboardVm
{
    public List<Order> CheckoutOrders { get; set; } = [];
    public List<Order> BulkRequests { get; set; } = [];
    public List<ContactRequest> Inquiries { get; set; } = [];
    public List<Booking> Bookings { get; set; } = [];
    public List<ProductSummaryResponse> Products { get; set; } = [];
    public CatalogueFacets Facets { get; set; } = CatalogueFacets.Empty;
    public List<NewsPost> NewsPosts { get; set; } = [];
    public List<MediaAsset> GalleryImages { get; set; } = [];
    public List<ReviewResponse> Reviews { get; set; } = [];
    public List<RetailPartner> Partners { get; set; } = [];
    public SiteSettingsResponse Settings { get; set; } = new(0, "ZAR", 0, 30, true, string.Empty, false, string.Empty, DateTime.UtcNow);
    public List<User> AdminUsers { get; set; } = [];
    public ProductFilter ProductFilter { get; set; } = new();
}
