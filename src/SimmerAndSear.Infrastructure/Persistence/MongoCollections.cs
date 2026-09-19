namespace SimmerAndSear.Infrastructure.Persistence;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. C# classes [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/classes> [Accessed 18 September 2026].
//
//MongoDB, n.d. CRUD Operations [Source code].
//Available at: <https://www.mongodb.com/docs/drivers/csharp/current/crud/> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public static class MongoCollections
{
    public const string Products = "products";
    public const string Users = "users";
    public const string AuditLog = "auditLog";
    public const string SiteSettings = "siteSettings";
    public const string MediaAssets = "mediaAssets";
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
