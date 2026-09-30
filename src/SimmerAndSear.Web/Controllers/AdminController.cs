using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Admin;
using SimmerAndSear.Application.Catalogue;
using SimmerAndSear.Contracts.Admin;
using SimmerAndSear.Contracts.Catalogue;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Web.Security;
using SimmerAndSear.Web.ViewModels;

namespace SimmerAndSear.Web.Controllers;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Handle requests with controllers in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Policy-based authorization in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Views in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
[Authorize(Policy = Permissions.Staff)]
public class AdminController(
    IAuditLogRepository audit,
    IOrderRepository orders,
    IContactRepository contacts,
    IBookingRepository bookingRepo,
    IPartnerRepository partners,
    IGalleryRepository gallery,
    CatalogueService catalogue,
    IProductAttributeRepository attributes,
    IReviewRepository reviewRepo,
    ReviewService reviewService,
    INewsPostRepository newsRepo,
    NewsService newsService,
    SiteSettingsService settingsService,
    IUserRepository userRepo,
    IMediaAssetRepository mediaRepo,
    IMediaService mediaService,
    CurrentUser current,
    AuditWriter auditWriter,
    IPasswordHasher hasher) : Controller
{
    public async Task<IActionResult> Index([FromQuery] ProductFilter productFilter, CancellationToken ct = default)
    {
        ViewData["Title"] = "Admin Dashboard";


        var isMaster = User.IsInRole(nameof(RoleName.MasterAdmin));

        var ordersPage = await orders.ListAsync(1, 200, ct);

        var checkoutOrders = ordersPage.Items.Where(o => o.OrderType == OrderType.Checkout).ToList();
        var bulkRequests = ordersPage.Items.Where(o => o.OrderType == OrderType.QuoteRequest).ToList();

        var inquiries = await contacts.ListAsync(ct);
        var bookings = await bookingRepo.ListAsync(ct);
        var reviewsPage = await reviewService.ListAsync(1, 100, ct: ct);
        var partnerList = await partners.ListAsync(ct);

        productFilter = (productFilter ?? new ProductFilter()) with { PageSize = 100 };

        var vm = new AdminDashboardVm
        {
            CheckoutOrders = checkoutOrders,
            BulkRequests = bulkRequests,
            Inquiries = inquiries,
            Bookings = bookings,
            Reviews = reviewsPage.Items.ToList(),
            Partners = partnerList,
            ProductFilter = productFilter,
            ShowMasterSections = isMaster
        };

        if (!isMaster)
        {
            return View(vm);
        }

        var productsPage = await catalogue.SearchAsync(productFilter, includeUnpublished: true, ct);
        var facets = await catalogue.GetFacetsAsync(includeUnpublished: true, ct);
        var saved = await attributes.GetAsync(ct);
        static string[] Merge(IReadOnlyList<string> a, IReadOnlyList<string> b) =>
            a.Concat(b).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        facets = new CatalogueFacets(Merge(facets.Brands, saved.Brands), Merge(facets.Categories, saved.Categories),
            Merge(facets.CookwareTypes, saved.CookwareTypes), Merge(facets.Materials, saved.Materials), Merge(facets.SizeLabels, saved.SizeLabels));

        var newsPage = await newsRepo.ListAsync(1, 100, ct);
        var usersPage = await userRepo.ListAsync(1, 100, ct);

        vm.Products = productsPage.Items.ToList();
        vm.Facets = facets;
        vm.NewsPosts = newsPage.Items.ToList();
        vm.GalleryImages = await gallery.ListAsync(ct);
        vm.Settings = await settingsService.GetAsync(ct);
        vm.AdminUsers = usersPage.Items.Where(u => u.UserType == UserType.Admin).ToList();

        return View(vm);
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Login()
    {
        return RedirectToAction("Login", "Auth", new { returnUrl = "/Admin" });
    }

    [HttpGet, Authorize(Policy = nameof(Permission.AuditRead))]
    public async Task<IActionResult> Audit([Range(1, int.MaxValue)] int pageNumber = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return BadRequest();
        return View(await audit.ListAsync(pageNumber, 20, ct));
    }

    // ── Order Operations ─────────────────────────────────────────────────────

    [HttpPost("Admin/Orders/{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(string id, [FromForm] OrderStatus status, CancellationToken ct)
    {
        var order = await orders.GetAsync(id, ct);
        if (order is null) return NotFound();

        var oldStatus = order.Status;
        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        await orders.UpdateAsync(order, ct);

        await auditWriter.WriteAsync(
            AuditAction.Update,
            $"Order {order.OrderReference} status changed from {oldStatus} to {status}");

        if (Request.Headers.Accept.ToString().Contains("application/json"))
        {
            return Ok(new { success = true, status = status.ToString() });
        }
        return Redirect("/Admin#orders");
    }

    [HttpPost("Admin/Orders/{id}/delete")]
    public async Task<IActionResult> DeleteOrder(string id, CancellationToken ct)
    {
        var order = await orders.GetAsync(id, ct);
        if (order is not null)
        {
            await orders.DeleteAsync(id, ct);
            await auditWriter.WriteAsync(AuditAction.Delete, $"Order deleted: {order.OrderReference}");
        }

        if (Request.Headers.Accept.ToString().Contains("application/json"))
        {
            return Ok(new { success = true });
        }
        return Redirect("/Admin#orders");
    }

    // ── Inquiry Operations ───────────────────────────────────────────────────

    [HttpPost("Admin/Inquiries/{id}/delete"), Authorize(Policy = nameof(Permission.InquiryManage))]
    public async Task<IActionResult> DeleteInquiry(string id, CancellationToken ct)
    {
        await contacts.DeleteAsync(id, ct);
        await auditWriter.WriteAsync(AuditAction.Delete, $"Contact inquiry deleted: {id}");

        if (Request.Headers.Accept.ToString().Contains("application/json"))
        {
            return Ok(new { success = true });
        }
        return Redirect("/Admin#inquiries");
    }

    // ── Catalogue Operations ─────────────────────────────────────────────────

    [HttpGet("Admin/Products/{id}"), Authorize(Policy = nameof(Permission.CatalogueManage))]
    public async Task<IActionResult> GetProduct(string id, CancellationToken ct)
    {
        var product = await catalogue.GetAsync(id, true, ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost("Admin/Attributes"), Authorize(Policy = nameof(Permission.CatalogueManage))]
    public async Task<IActionResult> AddAttribute([FromForm] string field, [FromForm] string value, CancellationToken ct)
    {
        var maxLength = field switch { "brand" or "category" or "cookwareType" => 80, "material" => 120, "sizeLabel" => 40, _ => 0 };
        if (maxLength == 0 || string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
            return BadRequest(new { message = "Enter a valid attribute value." });
        await attributes.AddAsync(field, value.Trim(), ct);
        await auditWriter.WriteAsync(AuditAction.Create, $"Product attribute added: {field} ({value.Trim()})");
        return Ok(new { value = value.Trim() });
    }

    [HttpPost("Admin/Products/Create")]
    [HttpPost("Admin/Products/{id}/edit")]
    [Authorize(Policy = nameof(Permission.CatalogueManage))]
    public async Task<IActionResult> SaveProduct([FromForm] UpdateProductRequest request, IFormFile? imageFile, CancellationToken ct, string? id = null)
    {
        if (id is null && string.IsNullOrWhiteSpace(request.Sku))
        {
            request.Sku = $"SKU-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            ModelState.Remove(nameof(request.Sku));
        }
        if (!ModelState.IsValid) return BadRequest(new { message = "Check the product fields. The name needs at least 2 characters, the description at least 10 characters, and the price must be between 0.01 and 1000000." });
        var existing = id is null ? null : await catalogue.GetAsync(id, true, ct);
        if (id is not null && existing is null) return NotFound();
        request.IsPublished = existing?.IsPublished ?? true;
        request.CapacityLitres = existing?.CapacityLitres;
        request.Images = existing?.Images.Select(i => new ProductImageRequest
        {
            MediaAssetId = i.MediaAssetId, AltText = i.AltText, DisplayOrder = i.DisplayOrder, IsPrimary = i.IsPrimary
        }).ToList() ?? [];
        try
        {
            if (imageFile is { Length: > 0 })
            {
                await using var stream = imageFile.OpenReadStream();
                var asset = await mediaService.UploadAsync(stream, imageFile.FileName, imageFile.Length, current.Id, ct);
                request.Images = [new ProductImageRequest { MediaAssetId = asset.Id, AltText = request.Name, IsPrimary = true }];
            }
            if (id is null) await catalogue.CreateAsync(request, ct);
            else await catalogue.UpdateAsync(id, request, ct);
            await auditWriter.WriteAsync(id is null ? AuditAction.Create : AuditAction.Update, $"Product saved: {request.Sku} ({request.Name})");
            if (Request.Headers.Accept.ToString().Contains("application/json")) return Ok(new { success = true });
            return Redirect("/Admin#products");
        }
        catch (Exception ex) when (ex is ProductRuleException or MediaValidationException or DuplicateSkuException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("Admin/Products/{id}/delete"), Authorize(Policy = nameof(Permission.CatalogueManage))]
    public async Task<IActionResult> DeleteProduct(string id, CancellationToken ct)
    {
        await catalogue.DeleteAsync(id, ct);
        await auditWriter.WriteAsync(AuditAction.Delete, $"Product deleted: {id}");

        if (Request.Headers.Accept.ToString().Contains("application/json"))
        {
            return Ok(new { success = true });
        }
        return Redirect("/Admin#products");
    }

    // ── Journal Operations ───────────────────────────────────────────────────

    [HttpGet("Admin/Journal/{id}")]
    public async Task<IActionResult> GetJournal(string id, CancellationToken ct)
    {
        var post = await newsService.GetAsync(id, ct);
        return post is null ? NotFound() : Ok(post);
    }

    [HttpPost("Admin/Journal/Create")]
    [HttpPost("Admin/Journal/{id}/edit")]
    public async Task<IActionResult> SaveJournal([FromForm] string title, [FromForm] string content, IFormFile? imageFile, CancellationToken ct, string? id = null)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            return BadRequest(new { message = "Title and content are required." });
        var existing = id is null ? null : await newsService.GetAsync(id, ct);
        if (id is not null && existing is null) return NotFound();
        var imageAssetId = existing?.ImageMediaAssetId;
        if (id is null && imageFile is not { Length: > 0 })
            return BadRequest(new { message = "Choose an image for the journal entry." });
        try
        {
            if (imageFile is { Length: > 0 })
            {
                await using var stream = imageFile.OpenReadStream();
                var asset = await mediaService.UploadAsync(stream, imageFile.FileName, imageFile.Length, current.Id, ct);
                imageAssetId = asset.Id;
            }
            if (id is null)
            {
                var post = await newsService.CreateAsync(new CreateNewsPostRequest { Title = title, Body = content, ImageMediaAssetId = imageAssetId }, current.Id ?? "system", current.Name, ct);
                await newsService.PublishAsync(post.Id, ct);
            }
            else await newsService.UpdateAsync(id, new UpdateNewsPostRequest { Title = title, Body = content, ImageMediaAssetId = imageAssetId }, ct);
            await auditWriter.WriteAsync(id is null ? AuditAction.Create : AuditAction.Update, $"Journal entry saved: {title.Trim()}");
            if (Request.Headers.Accept.ToString().Contains("application/json")) return Ok(new { success = true });
            return Redirect("/Admin#journal");
        }
        catch (MediaValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("Admin/Journal/{id}/delete")]
    public async Task<IActionResult> DeleteJournal(string id, CancellationToken ct)
    {
        await newsService.DeleteAsync(id, ct);
        await auditWriter.WriteAsync(AuditAction.Delete, $"Journal entry deleted: {id}");

        if (Request.Headers.Accept.ToString().Contains("application/json"))
        {
            return Ok(new { success = true });
        }
        return Redirect("/Admin#journal");
    }

    // ── Gallery Operations ───────────────────────────────────────────────────


    [HttpPost("Admin/Gallery/{slot:int}"), Authorize(Policy = nameof(Permission.CatalogueManage))]
    public async Task<IActionResult> SaveGalleryImage(
        int slot, IFormFile? file, string? caption, CancellationToken ct)
    {
        if (!GalleryImage.IsValidSlot(slot))
        {
            return BadRequest(new { message = $"The gallery has {GalleryImage.SlotCount} positions." });
        }

        var existing = await gallery.GetBySlotAsync(slot, ct);

        if (file is not { Length: > 0 } && existing is null)
        {
            return BadRequest(new { message = "Choose an image for this position." });
        }


        caption = caption?.Trim();
        if (string.IsNullOrWhiteSpace(caption))
        {
            return BadRequest(new
            {
                message = "Describe the image. The caption is what a visitor using a screen reader hears."
            });
        }

        var mediaAssetId = existing?.MediaAssetId;
        string? replaced = null;

        if (file is { Length: > 0 })
        {
            try
            {
                await using var stream = file.OpenReadStream();
                var asset = await mediaService.UploadAsync(stream, file.FileName, file.Length, current.Id, ct);
                replaced = mediaAssetId;
                mediaAssetId = asset.Id;
            }
            catch (MediaValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        await gallery.SetAsync(new GalleryImage
        {
            Slot = slot,
            MediaAssetId = mediaAssetId!,
            Caption = caption,
            UpdatedBy = current.Id ?? string.Empty
        }, ct);

    
        if (replaced is { Length: > 0 })
        {
            await mediaRepo.DeleteAsync(replaced, ct);
        }

        await auditWriter.WriteAsync(AuditAction.Update, $"Gallery position {slot} set to {mediaAssetId}");

        if (Request.Headers.Accept.ToString().Contains("application/json"))
        {
            return Ok(new { success = true });
        }
        return Redirect("/Admin#gallery");
    }

    [HttpPost("Admin/Gallery/{slot:int}/clear"), Authorize(Policy = nameof(Permission.CatalogueManage))]
    public async Task<IActionResult> ClearGalleryImage(int slot, CancellationToken ct)
    {
        var existing = await gallery.GetBySlotAsync(slot, ct);
        await gallery.ClearAsync(slot, ct);

        if (existing is { MediaAssetId.Length: > 0 })
        {
            await mediaRepo.DeleteAsync(existing.MediaAssetId, ct);
        }

        await auditWriter.WriteAsync(AuditAction.Delete, $"Gallery position {slot} cleared");

        if (Request.Headers.Accept.ToString().Contains("application/json"))
        {
            return Ok(new { success = true });
        }
        return Redirect("/Admin#gallery");
    }

    // ── Review Operations ────────────────────────────────────────────────────

    [HttpPost("Admin/Reviews/{id}/approve"), Authorize(Policy = nameof(Permission.ReviewManage))]
    public async Task<IActionResult> ApproveReview(string id, CancellationToken ct)
    {
        await reviewService.ApproveAsync(id, current.Id ?? "system", ct);
        await auditWriter.WriteAsync(AuditAction.Update, $"Review approved: {id}");

        if (Request.Headers.Accept.ToString().Contains("application/json"))
        {
            return Ok(new { success = true });
        }
        return Redirect("/Admin#reviews");
    }

    [HttpPost("Admin/Reviews/{id}/reject"), Authorize(Policy = nameof(Permission.ReviewManage))]
    public async Task<IActionResult> RejectReview(string id, CancellationToken ct)
    {
        await reviewService.RejectAsync(id, current.Id ?? "system", ct);
        await auditWriter.WriteAsync(AuditAction.Update, $"Review rejected: {id}");

        if (Request.Headers.Accept.ToString().Contains("application/json"))
        {
            return Ok(new { success = true });
        }
        return Redirect("/Admin#reviews");
    }

    [HttpPost("Admin/Reviews/{id}/delete"), Authorize(Policy = nameof(Permission.ReviewManage))]
    public async Task<IActionResult> DeleteReview(string id, CancellationToken ct)
    {
        await reviewRepo.DeleteAsync(id, ct);
        await auditWriter.WriteAsync(AuditAction.Delete, $"Review deleted: {id}");

        if (Request.Headers.Accept.ToString().Contains("application/json"))
        {
            return Ok(new { success = true });
        }
        return Redirect("/Admin#reviews");
    }

    // ── Partner Operations ───────────────────────────────────────────────────

    [HttpPost("Admin/Partners/Create")]
    public async Task<IActionResult> CreatePartner(
        [FromForm] string name,
        [FromForm] string location,
        [FromForm] string? description,
        CancellationToken ct)
    {
        var partner = new RetailPartner
        {
            Name = name?.Trim() ?? "New Partner",
            Location = location?.Trim() ?? "South Africa",
            Description = description?.Trim() ?? string.Empty
        };

        await partners.CreateAsync(partner, ct);
        await auditWriter.WriteAsync(AuditAction.Create, $"Retail partner created: {partner.Name}");
        return Redirect("/Admin#partners");
    }

    [HttpPost("Admin/Partners/{id}/delete")]
    public async Task<IActionResult> DeletePartner(string id, CancellationToken ct)
    {
        await partners.DeleteAsync(id, ct);
        await auditWriter.WriteAsync(AuditAction.Delete, $"Retail partner deleted: {id}");

        if (Request.Headers.Accept.ToString().Contains("application/json"))
        {
            return Ok(new { success = true });
        }
        return Redirect("/Admin#partners");
    }

    // ── Settings Operations ──────────────────────────────────────────────────

    [HttpPost("Admin/Settings"), Authorize(Policy = nameof(Permission.SettingsManage))]
    public async Task<IActionResult> UpdateSettings(
        [FromForm] decimal bulkOrderThreshold,
        [FromForm] string? salesNotificationEmail,
        [FromForm] decimal shippingFlatRate,
        [FromForm] string? displayCurrency,
        CancellationToken ct)
    {
        var currentSettings = await settingsService.GetAsync(ct);
        var req = new UpdateSiteSettingsRequest
        {
            BulkOrderThreshold = bulkOrderThreshold > 0 ? bulkOrderThreshold : currentSettings.BulkOrderThreshold,
            SalesNotificationEmail = string.IsNullOrWhiteSpace(salesNotificationEmail) ? currentSettings.SalesNotificationEmail : salesNotificationEmail.Trim(),
            ShippingFlatRate = shippingFlatRate >= 0 ? shippingFlatRate : currentSettings.ShippingFlatRate,
            DisplayCurrency = string.IsNullOrWhiteSpace(displayCurrency) ? currentSettings.DisplayCurrency : displayCurrency.Trim(),
            QuotationValidityDays = currentSettings.QuotationValidityDays,
            ReviewsRequirePurchase = currentSettings.ReviewsRequirePurchase,
            MaintenanceMode = currentSettings.MaintenanceMode
        };

        await settingsService.UpdateAsync(req, current.Id ?? "system", ct);
        await auditWriter.WriteAsync(AuditAction.Update, "System settings updated");
        return Redirect("/Admin#settings");
    }

    // ── Admin Users Operations ───────────────────────────────────────────────

    [HttpPost("Admin/Users/Create"), Authorize(Policy = nameof(Permission.AccountManage))]
    public async Task<IActionResult> CreateAdminUser(
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] string role,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            TempData["Error"] = "Email and password are required.";
            return Redirect("/Admin#admins");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existing = await userRepo.GetByEmailAsync(normalizedEmail, ct);
        if (existing is not null)
        {
            TempData["Error"] = "A user with this email already exists.";
            return Redirect("/Admin#admins");
        }

        var assignedRole = role == "MasterAdmin" ? RoleName.MasterAdmin : RoleName.SalesAdmin;

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = hasher.Hash(password),
            FirstName = role == "MasterAdmin" ? "Master" : "Sales",
            LastName = "Admin",
            UserType = UserType.Admin,
            Role = assignedRole,
            IsActive = true
        };

        await userRepo.CreateAsync(user, ct);
        await auditWriter.WriteAsync(AuditAction.Create, $"Admin user created: {user.Email} ({user.Role})");
        return Redirect("/Admin#admins");
    }

    [HttpPost("Admin/Users/{id}/delete"), Authorize(Policy = nameof(Permission.AccountManage))]
    public async Task<IActionResult> DeleteAdminUser(string id, CancellationToken ct)
    {
        if (id == current.Id)
        {
            TempData["Error"] = "Cannot delete the currently signed in administrator.";
            return Redirect("/Admin#admins");
        }

        await userRepo.DeleteAsync(id, ct);
        await auditWriter.WriteAsync(AuditAction.Delete, $"Admin user deleted: {id}");

        if (Request.Headers.Accept.ToString().Contains("application/json"))
        {
            return Ok(new { success = true });
        }
        return Redirect("/Admin#admins");
    }
}
