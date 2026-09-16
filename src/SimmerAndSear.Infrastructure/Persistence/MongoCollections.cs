namespace SimmerAndSear.Infrastructure.Persistence;

/// <summary>
/// Collection names in one place, so a typo is a compile error rather than a
/// silently-created empty collection.
/// </summary>
public static class MongoCollections
{
    public const string Products = "products";
    public const string Users = "users";
    public const string AuditLog = "auditLog";
    public const string SiteSettings = "siteSettings";
    public const string MediaAssets = "mediaAssets";

    // Created by their slice owners in stages 3 and 4; named here so everyone
    // agrees on the spelling before the first document is written.
    public const string Orders = "orders";
    public const string Carts = "carts";
    public const string Wishlists = "wishlists";
    public const string Reviews = "reviews";
    public const string NewsPosts = "newsPosts";
    public const string ContactRequests = "contactRequests";
    public const string Bookings = "bookings";
    public const string GalleryImages = "galleryImages";
    public const string RetailPartners = "retailPartners";
    public const string QuotationDocuments = "quotationDocuments";
}
