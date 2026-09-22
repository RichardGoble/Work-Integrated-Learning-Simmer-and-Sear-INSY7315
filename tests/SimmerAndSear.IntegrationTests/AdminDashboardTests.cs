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
            var invalid = await Post("/Admin/Products/Create", fields, "imageFile");
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            Assert.Contains("10 characters", await invalid.Content.ReadAsStringAsync());
            fields["description"] = "A durable copper pan for testing.";
            var create = await Post("/Admin/Products/Create", fields, "imageFile");
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
            fields["price"] = "-1";
            Assert.Equal(HttpStatusCode.BadRequest, (await Post($"/Admin/Products/{product.Id}/edit", fields)).StatusCode);

            Assert.Equal(HttpStatusCode.Redirect, (await Post("/Admin/Journal/Create", new() { ["title"] = "Kitchen News", ["content"] = "Fresh journal content." }, "imageFile")).StatusCode);
            var news = scope.ServiceProvider.GetRequiredService<INewsPostRepository>();
            var post = Assert.Single((await news.ListAsync(1, 10)).Items);
            Assert.Equal(PostStatus.Published, post.Status);
            Assert.Equal(HttpStatusCode.Redirect, (await Post($"/Admin/Journal/{post.Id}/edit", new() { ["title"] = "Updated News", ["content"] = "Updated journal content." })).StatusCode);
            Assert.Equal("Updated News", (await news.GetAsync(post.Id))!.Title);
            Assert.Equal(HttpStatusCode.Redirect, (await Post("/Admin/Gallery/Create", new(), "file")).StatusCode);
            var media = scope.ServiceProvider.GetRequiredService<IMediaAssetRepository>();
            Assert.Equal(3, (await media.ListAsync()).Count);

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
            var image = (await media.ListAsync())[0];
            Assert.Equal(HttpStatusCode.Redirect, (await Post($"/Admin/Gallery/{image.Id}/delete", new())).StatusCode);
            Assert.Null(await media.GetAsync(image.Id));
        }
        finally
        {
            await factory.Services.GetRequiredService<IMongoClient>().DropDatabaseAsync(databaseName);
        }
    }
}
