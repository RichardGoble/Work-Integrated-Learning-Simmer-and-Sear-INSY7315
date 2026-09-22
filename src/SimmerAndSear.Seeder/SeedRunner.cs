using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Media;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Infrastructure.Media;

namespace SimmerAndSear.Seeder;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2025. How to deserialize JSON in C# [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/deserialization> [Accessed 18 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class SeedRunner(
    IProductRepository products,
    IUserRepository users,
    ISiteSettingsRepository settings,
    IMediaAssetRepository media,
    IFileStore files,
    IPasswordHasher hasher,
    IOptions<SeedOptions> options,
    ILogger<SeedRunner> logger,
    IMediaService? mediaService = null)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly SeedOptions _options = options.Value;
    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        if (_options.AccountsOnly) return await SeedAccountsAsync(ct);
        await SeedSettingsAsync(ct);
        var accountProblems = await SeedAccountsAsync(ct);
        var catalogueProblems = await SeedCatalogueAsync(ct);

        return accountProblems + catalogueProblems;
    }

    private async Task SeedSettingsAsync(CancellationToken ct)
    {
        var current = await settings.GetAsync(ct);
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
            var existing = await users.GetByEmailAsync(account.Email.Trim(), ct);
            if (existing is not null)
            {
                if (!string.IsNullOrWhiteSpace(account.Password) && account.Password.Length is >= 12 and <= 128)
                {
                    existing.PasswordHash = hasher.Hash(account.Password);
                    existing.IsActive = true;
                    existing.FailedLoginAttempts = 0;
                    existing.LockedOutUntil = null;
                    existing.Role = account.Role;
                    existing.UserType = account.Type;
                    await users.UpdateAsync(existing, ct);
                    logger.LogInformation("Updated {Role} account {Email} password.", account.Role, account.Email);
                }
                continue;
            }
            if (string.IsNullOrWhiteSpace(account.Password) || account.Password.Length is < 12 or > 128)
            {
                logger.LogError(
                    "Set Seed__{Key}Password to a password of 12-128 characters for {Email}.",
                    account.Role, account.Email);

                problems++;
                continue;
            }

            await users.CreateAsync(new User
            {
                Email = account.Email.Trim().ToLowerInvariant(),
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
        var fileLength = new FileInfo(path).Length;
        var service = mediaService ?? new MediaService(media, files, new ImageSharpImageProcessor());
        var asset = await service.UploadAsync(stream, fileName, fileLength, "seeder", ct);
        return asset.Id;
    }
    // ── References ──────────────────────────────────────────────────────────────
    //Microsoft, 2026. C# record types [Source code].
    //Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records> [Accessed 18 September 2026].
    //
    //Microsoft, 2025. How to deserialize JSON in C# [Source code].
    //Available at: <https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/deserialization> [Accessed 18 September 2026].
    // ────────────────────────────────────────────────────────────────────────────
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
