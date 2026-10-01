using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Catalogue;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Infrastructure.Persistence;
using SimmerAndSear.IntegrationTests.Security;

namespace SimmerAndSear.IntegrationTests;

public sealed class AdminDashboardTests
{
    [Fact]
    public async Task Admin_controls_save_edit_filter_and_delete_records()
    {
        using var security = new SecurityFactory();
        var databaseName = "admin_controls_" + Guid.NewGuid().ToString("N");
        using var factory = security.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.PostConfigure<MongoOptions>(options => { options.Database = databaseName; options.ApplySchemaOnStartup = true; })));
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var user = security.AddUser(RoleName.MasterAdmin);
        try
        {
            var csrf = await client.GetFromJsonAsync<JsonElement>("/api/accounts/csrf");
            using var login = new HttpRequestMessage(HttpMethod.Post, "/api/accounts/login")
            {
                Content = JsonContent.Create(new { user.Email, password = SecurityTests.Password })
            };
            login.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
            (await client.SendAsync(login)).EnsureSuccessStatusCode();
            csrf = await client.GetFromJsonAsync<JsonElement>("/api/accounts/csrf");
            client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());

            async Task<HttpResponseMessage> Post(string path, Dictionary<string, string> fields, string? fileField = null)
            {
                using var body = new MultipartFormDataContent();
                body.Add(new StringContent(csrf.GetProperty("token").GetString()!), "__RequestVerificationToken");
                foreach (var (key, value) in fields) body.Add(new StringContent(value), key);
                if (fileField is not null)
                {
                    using var image = new Image<Rgba32>(2, 2);
                    using var stream = new MemoryStream();
                    image.SaveAsPng(stream);
                    var bytes = stream.ToArray();
                    body.Add(new ByteArrayContent(bytes), fileField, "test.png");
                }
                return await client.PostAsync(path, body);
            }

            (await Post("/Admin/Attributes", new() { ["field"] = "material", ["value"] = "Copper" })).EnsureSuccessStatusCode();
            (await Post("/Admin/Attributes", new() { ["field"] = "material", ["value"] = "copper" })).EnsureSuccessStatusCode();
            using var scope = factory.Services.CreateScope();
            var attributes = await scope.ServiceProvider.GetRequiredService<IProductAttributeRepository>().GetAsync();
            Assert.Single(attributes.Materials);
            Assert.Equal("Copper", attributes.Materials[0]);

            var fields = new Dictionary<string, string>
            {
                ["name"] = "Copper Pan", ["description"] = "A durable copper pan for testing.",
                ["brand"] = "Test", ["category"] = "Classic", ["cookwareType"] = "Pan",
                ["material"] = "Copper", ["price"] = "250", ["stockQuantity"] = "0"
            };
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            fields["description"] = "Test";
            var invalid = await Post("/Admin/Products/Create", fields, "imageFiles");
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            Assert.Contains("10 characters", await invalid.Content.ReadAsStringAsync());
            fields["description"] = "A durable copper pan for testing.";
            var create = await Post("/Admin/Products/Create", fields, "imageFiles");
            Assert.True(create.StatusCode == HttpStatusCode.OK, await create.Content.ReadAsStringAsync());
            Assert.True((await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("success").GetBoolean());
            client.DefaultRequestHeaders.Accept.Clear();
            var products = scope.ServiceProvider.GetRequiredService<IProductRepository>();
            var page = await products.SearchAsync(new ProductFilter { SearchTerm = "Copper Pan" });
            var product = Assert.Single(page.Items);
            Assert.Equal(0, product.StockQuantity);
            fields["sku"] = product.Sku;
            fields["name"] = "Updated Copper Pan";
            Assert.Equal(HttpStatusCode.Redirect, (await Post($"/Admin/Products/{product.Id}/edit", fields)).StatusCode);
            var updated = await client.GetFromJsonAsync<ProductDetailResponse>($"/Admin/Products/{product.Id}");
            Assert.Equal("Updated Copper Pan", updated!.Name);
            Assert.Single(updated.Images);

    
            Assert.Equal(HttpStatusCode.Redirect,
                (await Post($"/Admin/Products/{product.Id}/edit", fields, "imageFiles")).StatusCode);
            var twoImages = await client.GetFromJsonAsync<ProductDetailResponse>($"/Admin/Products/{product.Id}");
            Assert.Equal(2, twoImages!.Images.Count);
            Assert.True(twoImages.Images[0].IsPrimary);
            Assert.False(twoImages.Images[1].IsPrimary);

            var second = twoImages.Images[1].MediaAssetId;
            Assert.Equal(HttpStatusCode.Redirect,
                (await Post($"/Admin/Products/{product.Id}/images/{second}/primary", new())).StatusCode);
            var promoted = await client.GetFromJsonAsync<ProductDetailResponse>($"/Admin/Products/{product.Id}");
            Assert.Equal(second, Assert.Single(promoted!.Images, i => i.IsPrimary).MediaAssetId);

            var demoted = promoted.Images.First(i => !i.IsPrimary).MediaAssetId;
            Assert.Equal(HttpStatusCode.Redirect,
                (await Post($"/Admin/Products/{product.Id}/images/{demoted}/delete", new())).StatusCode);
            var oneImage = await client.GetFromJsonAsync<ProductDetailResponse>($"/Admin/Products/{product.Id}");
            Assert.Single(oneImage!.Images);
            Assert.Null(await scope.ServiceProvider.GetRequiredService<IMediaAssetRepository>().GetAsync(demoted));

       
            var lastImage = oneImage.Images[0].MediaAssetId;
            var refused = await Post($"/Admin/Products/{product.Id}/images/{lastImage}/delete", new());
            Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
            Assert.Contains("at least one image", await refused.Content.ReadAsStringAsync());
            fields["price"] = "-1";
            Assert.Equal(HttpStatusCode.BadRequest, (await Post($"/Admin/Products/{product.Id}/edit", fields)).StatusCode);

            Assert.Equal(HttpStatusCode.Redirect, (await Post("/Admin/Journal/Create", new() { ["title"] = "Kitchen News", ["content"] = "Fresh journal content." }, "imageFile")).StatusCode);
            var news = scope.ServiceProvider.GetRequiredService<INewsPostRepository>();
            var post = Assert.Single((await news.ListAsync(1, 10)).Items);
            Assert.Equal(PostStatus.Published, post.Status);
            Assert.Equal(HttpStatusCode.Redirect, (await Post($"/Admin/Journal/{post.Id}/edit", new() { ["title"] = "Updated News", ["content"] = "Updated journal content." })).StatusCode);
            Assert.Equal("Updated News", (await news.GetAsync(post.Id))!.Title);
         
            Assert.Equal(HttpStatusCode.Redirect,
                (await Post("/Admin/Gallery/1", new() { ["caption"] = "Searing in a copper pan" }, "file")).StatusCode);
            var gallery = scope.ServiceProvider.GetRequiredService<IGalleryRepository>();
            var slot = Assert.Single(await gallery.ListAsync());
            Assert.Equal(1, slot.Slot);
            Assert.Equal("Searing in a copper pan", slot.Caption);
            var media = scope.ServiceProvider.GetRequiredService<IMediaAssetRepository>();
            Assert.Equal(3, (await media.ListAsync()).Count);

        
            Assert.Equal(HttpStatusCode.BadRequest,
                (await Post("/Admin/Gallery/2", new() { ["caption"] = " " }, "file")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,
                (await Post("/Admin/Gallery/9", new() { ["caption"] = "Out of range" }, "file")).StatusCode);
            Assert.Single(await gallery.ListAsync());

            var html = await client.GetStringAsync("/Admin?material=Copper");
            Assert.Contains("Updated Copper Pan", html);
            Assert.Contains("/js/admin-dashboard.js", html);
            Assert.DoesNotContain("onclick=\"toggleAdd", html);
            Assert.Contains("data-add-attribute=\"material\"", html);
            Assert.Contains("No products found", await client.GetStringAsync("/Admin?material=Missing"));

            var deleted = await Post($"/Admin/Products/{product.Id}/delete", new());
            Assert.Equal("/Admin#products", deleted.Headers.Location?.ToString());
            Assert.Null(await products.GetAsync(product.Id));
            Assert.Equal(HttpStatusCode.Redirect, (await Post($"/Admin/Journal/{post.Id}/delete", new())).StatusCode);
            Assert.Null(await news.GetAsync(post.Id));
     
            var galleryAssetId = (await gallery.GetBySlotAsync(1))!.MediaAssetId;
            Assert.Equal(HttpStatusCode.Redirect, (await Post("/Admin/Gallery/1/clear", new())).StatusCode);
            Assert.Empty(await gallery.ListAsync());
            Assert.Null(await media.GetAsync(galleryAssetId));
        }
        finally
        {
            await factory.Services.GetRequiredService<IMongoClient>().DropDatabaseAsync(databaseName);
        }
    }

    [Theory]
    [InlineData(RoleName.SalesAdmin, false)]
    [InlineData(RoleName.MasterAdmin, true)]
    public async Task Dashboard_shows_only_the_sections_the_role_can_act_on(RoleName role, bool master)
    {
        using var security = new SecurityFactory();
        var databaseName = "admin_roles_" + Guid.NewGuid().ToString("N");
        using var factory = security.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.PostConfigure<MongoOptions>(options => { options.Database = databaseName; options.ApplySchemaOnStartup = true; })));
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var user = security.AddUser(role);
        try
        {
            var csrf = await client.GetFromJsonAsync<JsonElement>("/api/accounts/csrf");
            using var login = new HttpRequestMessage(HttpMethod.Post, "/api/accounts/login")
            {
                Content = JsonContent.Create(new { user.Email, password = SecurityTests.Password })
            };
            login.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
            (await client.SendAsync(login)).EnsureSuccessStatusCode();

            csrf = await client.GetFromJsonAsync<JsonElement>("/api/accounts/csrf");
            client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());

            var html = await client.GetStringAsync("/Admin");

            foreach (var shared in new[] { "btn-orders", "btn-bulk", "btn-inquiries", "btn-reviews", "btn-partners" })
            {
                Assert.Contains($"id=\"{shared}\"", html);
            }

            foreach (var restricted in new[] { "btn-products", "btn-attributes", "btn-journal", "btn-gallery", "btn-settings", "btn-admins" })
            {
                if (master)
                {
                    Assert.Contains($"id=\"{restricted}\"", html);
                }
                else
                {
                    Assert.DoesNotContain($"id=\"{restricted}\"", html);
                }
            }

            using var settings = new MultipartFormDataContent
            {
                { new StringContent(csrf.GetProperty("token").GetString()!), "__RequestVerificationToken" },
                { new StringContent("9000"), "bulkOrderThreshold" },
                { new StringContent("120"), "shippingFlatRate" },
                { new StringContent("ZAR"), "displayCurrency" },
                { new StringContent("sales@simmerandsear.local"), "salesNotificationEmail" }
            };

            var saved = await client.PostAsync("/Admin/Settings", settings);

            Assert.Equal(
                master ? HttpStatusCode.Redirect : HttpStatusCode.Forbidden,
                saved.StatusCode);
        }
        finally
        {
            await factory.Services.GetRequiredService<IMongoClient>().DropDatabaseAsync(databaseName);
        }
    }
}
