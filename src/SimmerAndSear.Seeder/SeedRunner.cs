using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Seeder;

/// <summary>
/// Populates a database with the demonstration catalogue and the three role
/// accounts.
///
/// <para>
/// Idempotent by design: everything is matched on a natural key (SKU for
/// products, email for accounts, the fixed id for settings), so running it twice
/// changes nothing. That matters because it runs against staging and against a
/// developer's machine repeatedly, and a seeder that duplicates its own data is
/// worse than no seeder at all.
/// </para>
/// </summary>
public sealed class SeedRunner(
    IProductRepository products,
    IUserRepository users,
    ISiteSettingsRepository settings,
    IMediaAssetRepository media,
    IFileStore files,
    IPasswordHasher hasher,
    IOptions<SeedOptions> options,
    ILogger<SeedRunner> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly SeedOptions _options = options.Value;

    /// <summary>Returns the number of problems encountered; zero means a clean run.</summary>
    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        await SeedSettingsAsync(ct);
        var accountProblems = await SeedAccountsAsync(ct);
        var catalogueProblems = await SeedCatalogueAsync(ct);

        return accountProblems + catalogueProblems;
    }

    // ------------------------------------------------------------------ settings

    private async Task SeedSettingsAsync(CancellationToken ct)
    {
        var current = await settings.GetAsync(ct);

        // Only fill in a threshold on first run. Overwriting it every time would
        // undo whatever the client configured through the admin screen, which is
        // exactly the behaviour the "configurable without a code change"
        // requirement exists to prevent.
        if (current.BulkOrderThreshold > 0)
        {
            logger.LogInformation("Site settings already configured; left untouched.");
            return;
        }

        current.BulkOrderThreshold = _options.BulkOrderThreshold;
        current.ShippingFlatRate = _options.ShippingFlatRate;
        current.SalesNotificationEmail = _options.SalesNotificationEmail;
        current.DisplayCurrency = "ZAR";
        current.UpdatedBy = "seeder";

        await settings.UpdateAsync(current, ct);
        logger.LogInformation("Site settings seeded. Bulk-order threshold R{Threshold}.",
            _options.BulkOrderThreshold);
    }

    // ------------------------------------------------------------------ accounts

    private async Task<int> SeedAccountsAsync(CancellationToken ct)
    {
        var wanted = new[]
        {
            (Email: _options.MasterAdminEmail, Password: _options.MasterAdminPassword,
                Role: RoleName.MasterAdmin, Type: UserType.Admin, First: "Master", Last: "Admin"),
            (Email: _options.SalesAdminEmail, Password: _options.SalesAdminPassword,
                Role: RoleName.SalesAdmin, Type: UserType.Admin, First: "Sales", Last: "Admin"),
            (Email: _options.CustomerEmail, Password: _options.CustomerPassword,
                Role: RoleName.Customer, Type: UserType.Client, First: "Demo", Last: "Customer")
        };

        var problems = 0;

        foreach (var account in wanted)
        {
            if (string.IsNullOrWhiteSpace(account.Password))
            {
                logger.LogError(
                    "No password configured for {Email}. Set Seed__{Key} - the seeder will not " +
                    "invent a credential for an account that can administer the site.",
                    account.Email, account.Role);

                problems++;
                continue;
            }

            if (await users.GetByEmailAsync(account.Email, ct) is not null)
            {
                logger.LogInformation("Account {Email} already present.", account.Email);
                continue;
            }

            await users.CreateAsync(new User
            {
                Email = account.Email,
                PasswordHash = hasher.Hash(account.Password),
                FirstName = account.First,
                LastName = account.Last,
                Role = account.Role,
                UserType = account.Type,
                IsActive = true,
                Client = account.Type == UserType.Client ? new ClientProfile() : null
            }, ct);

            logger.LogInformation("Created {Role} account {Email}.", account.Role, account.Email);
        }

        return problems;
    }

    // ----------------------------------------------------------------- catalogue

    private async Task<int> SeedCatalogueAsync(CancellationToken ct)
    {
        var manifestPath = Path.Combine(AppContext.BaseDirectory, "SeedData", "products.json");

        if (!File.Exists(manifestPath))
        {
            logger.LogError("Product manifest not found at {Path}.", manifestPath);
            return 1;
        }

        var imagesPath = Path.IsPathRooted(_options.ImagesPath)
            ? _options.ImagesPath
            : Path.Combine(AppContext.BaseDirectory, _options.ImagesPath);

        if (!Directory.Exists(imagesPath) || Directory.GetFiles(imagesPath).Length == 0)
        {
            // Deliberately a hard stop rather than a placeholder image. Every
            // product must carry a real photograph - the database validator
            // enforces at least one image, and a catalogue of grey rectangles
            // throws away the visual-design marks the CSS has already earned.
            logger.LogError(
                "No product photography found in {Path}. Add the images named in " +
                "products.json and run again. Accounts and settings were still seeded.",
                imagesPath);

            return 1;
        }

        var manifest = JsonSerializer.Deserialize<List<SeedProduct>>(
            await File.ReadAllTextAsync(manifestPath, ct), Json) ?? [];

        var problems = 0;
        var created = 0;

        foreach (var item in manifest)
        {
            if (await products.GetBySkuAsync(item.Sku, ct) is not null)
            {
                continue;
            }

            var imageFile = Path.Combine(imagesPath, item.Image);

            if (!File.Exists(imageFile))
            {
                logger.LogWarning("Skipping {Sku}: image {Image} not found.", item.Sku, item.Image);
                problems++;
                continue;
            }

            var assetId = await UploadImageAsync(imageFile, item.Image, ct);

            await products.CreateAsync(new Product
            {
                Sku = item.Sku,
                Name = item.Name,
                Description = item.Description,
                Brand = item.Brand,
                Category = item.Category,
                CookwareType = item.CookwareType,
                Material = item.Material,
                SizeLabel = item.SizeLabel,
                CapacityLitres = item.CapacityLitres,
                Price = item.Price,
                Currency = "ZAR",
                StockQuantity = item.StockQuantity,
                IsPublished = true,
                Images =
                [
                    new ProductImage
                    {
                        ProductImageId = Guid.NewGuid().ToString("N"),
                        MediaAssetId = assetId,
                        AltText = item.AltText,
                        DisplayOrder = 0,
                        IsPrimary = true
                    }
                ]
            }, ct);

            created++;
        }

        logger.LogInformation("Catalogue seeded: {Created} new, {Total} in manifest.",
            created, manifest.Count);

        return problems;
    }

    private async Task<string> UploadImageAsync(string path, string fileName, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);

        var contentType = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        var gridFsId = await files.UploadAsync(stream, fileName, contentType, ct);

        return await media.CreateAsync(new MediaAsset
        {
            FileName = fileName,
            ContentType = contentType,
            FileSizeBytes = new FileInfo(path).Length,
            GridFsId = gridFsId,
            UploadedBy = "seeder"
        }, ct);
    }

    /// <summary>One entry in SeedData/products.json.</summary>
    private sealed record SeedProduct(
        string Sku,
        string Name,
        string Brand,
        string Category,
        string CookwareType,
        string? Material,
        string? SizeLabel,
        decimal? CapacityLitres,
        decimal Price,
        int StockQuantity,
        string Description,
        string Image,
        string AltText);
}
