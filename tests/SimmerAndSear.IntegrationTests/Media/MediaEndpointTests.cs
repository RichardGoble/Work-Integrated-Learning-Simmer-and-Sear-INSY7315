using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Media;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.IntegrationTests.Security;

namespace SimmerAndSear.IntegrationTests.Media;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Integration tests in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0> [Accessed 20 September 2026].
//
//xUnit.net, 2025. Getting Started with xUnit.net v2 [Source code].
//Available at: <https://xunit.net/docs/getting-started/v2/getting-started> [Accessed 20 September 2026].
//
//Shouldly, n.d. Shouldly Documentation [Source code].
//Available at: <https://docs.shouldly.org/> [Accessed 20 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class MediaEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<JsonElement>("/api/accounts/csrf");
        return response.GetProperty("token").GetString()!;
    }

    private static async Task LoginAsync(HttpClient client, User user)
    {
        var token = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/accounts/login")
        {
            Content = JsonContent.Create(new { user.Email, password = SecurityTests.Password })
        };
        request.Headers.Add("X-CSRF-TOKEN", token);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static MultipartFormDataContent CreateImageContent(
        int width, int height, string fileName, string format = "jpeg")
    {
        using var image = new Image<Rgba32>(width, height);
        using var ms = new MemoryStream();
        if (format.Equals("png", StringComparison.OrdinalIgnoreCase))
        {
            image.Save(ms, new PngEncoder());
        }
        else
        {
            image.Save(ms, new JpegEncoder());
        }

        var bytes = ms.ToArray();
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(format.Equals("png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg");
        content.Add(fileContent, "file", fileName);
        return content;
    }

    [Fact]
    public async Task Anonymous_user_cannot_upload_media()
    {
        using var factory = new MediaEndpointTestFactory();
        using var client = factory.Browser();

        using var content = CreateImageContent(400, 300, "sample.jpg");
        var token = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/media") { Content = content };
        request.Headers.Add("X-CSRF-TOKEN", token);

        var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Customer_user_is_forbidden_from_uploading_media()
    {
        using var factory = new MediaEndpointTestFactory();
        using var client = factory.Browser();
        var customer = factory.AddUser(RoleName.Customer);
        await LoginAsync(client, customer);

        using var content = CreateImageContent(400, 300, "sample.jpg");
        var token = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/media") { Content = content };
        request.Headers.Add("X-CSRF-TOKEN", token);

        var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MasterAdmin_can_upload_valid_media_and_response_has_location_and_no_internal_ids()
    {
        using var factory = new MediaEndpointTestFactory();
        using var client = factory.Browser();
        var admin = factory.AddUser(RoleName.MasterAdmin);
        await LoginAsync(client, admin);

        using var content = CreateImageContent(800, 600, "catalogue.jpg", "jpeg");
        var token = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/media") { Content = content };
        request.Headers.Add("X-CSRF-TOKEN", token);

        var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.OriginalString.ShouldStartWith("/api/media/");

        var dto = await response.Content.ReadFromJsonAsync<MediaResponse>(JsonOptions);
        dto.ShouldNotBeNull();
        dto.Id.ShouldNotBeNullOrWhiteSpace();
        dto.FileName.ShouldBe("catalogue.jpg");
        dto.ContentType.ShouldBe("image/jpeg");
        dto.Width.ShouldBe(800);
        dto.Height.ShouldBe(600);
        dto.OriginalUrl.ShouldBe($"/api/media/{dto.Id}");
        dto.ThumbnailUrl.ShouldBe($"/api/media/{dto.Id}/thumb");

        // Verify audit log entry
        factory.Audit.Items.ShouldContain(a =>
            a.Action == AuditAction.Create &&
            a.Summary.Contains("Media uploaded") &&
            a.Summary.Contains(dto.Id));

        // Verify media exists in repository
        var stored = await factory.MediaRepository.GetAsync(dto.Id);
        stored.ShouldNotBeNull();
        stored.GridFsId.ShouldNotBeNullOrWhiteSpace();
        stored.ThumbnailGridFsId.ShouldNotBeNullOrWhiteSpace();
        stored.GridFsId.ShouldNotBe(stored.ThumbnailGridFsId);
    }

    [Fact]
    public async Task Upload_empty_file_returns_400_bad_request()
    {
        using var factory = new MediaEndpointTestFactory();
        using var client = factory.Browser();
        var admin = factory.AddUser(RoleName.MasterAdmin);
        await LoginAsync(client, admin);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([]), "file", "empty.jpg");
        var token = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/media") { Content = content };
        request.Headers.Add("X-CSRF-TOKEN", token);

        var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_script_renamed_to_jpg_is_rejected_with_400()
    {
        using var factory = new MediaEndpointTestFactory();
        using var client = factory.Browser();
        var admin = factory.AddUser(RoleName.MasterAdmin);
        await LoginAsync(client, admin);

        var evilPayload = System.Text.Encoding.UTF8.GetBytes("alert('malicious payload');");
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(evilPayload), "file", "exploit.jpg");
        var token = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/media") { Content = content };
        request.Headers.Add("X-CSRF-TOKEN", token);

        var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("not a valid");
    }

    [Fact]
    public async Task Get_original_streams_directly_with_immutable_cache_control_and_etag()
    {
        using var factory = new MediaEndpointTestFactory();
        using var client = factory.Browser();
        var admin = factory.AddUser(RoleName.MasterAdmin);
        await LoginAsync(client, admin);

        using var content = CreateImageContent(600, 400, "pan.jpg", "jpeg");
        var token = await GetCsrfTokenAsync(client);
        using var uploadReq = new HttpRequestMessage(HttpMethod.Post, "/api/media") { Content = content };
        uploadReq.Headers.Add("X-CSRF-TOKEN", token);
        var uploadRes = await client.SendAsync(uploadReq);
        var dto = (await uploadRes.Content.ReadFromJsonAsync<MediaResponse>(JsonOptions))!;

        // Anonymous GET of original image
        var getRes = await client.GetAsync($"/api/media/{dto.Id}");
        getRes.StatusCode.ShouldBe(HttpStatusCode.OK);
        getRes.Content.Headers.ContentType!.MediaType.ShouldBe("image/jpeg");
        getRes.Headers.CacheControl!.ToString().ShouldContain("public");
        getRes.Headers.CacheControl!.ToString().ShouldContain("immutable");
        getRes.Headers.ETag.ShouldNotBeNull();

        // 304 Not Modified when sending matching If-None-Match
        using var cachedReq = new HttpRequestMessage(HttpMethod.Get, $"/api/media/{dto.Id}");
        cachedReq.Headers.IfNoneMatch.Add(getRes.Headers.ETag!);
        var cachedRes = await client.SendAsync(cachedReq);
        cachedRes.StatusCode.ShouldBe(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task Get_thumbnail_streams_thumbnail_with_caching()
    {
        using var factory = new MediaEndpointTestFactory();
        using var client = factory.Browser();
        var admin = factory.AddUser(RoleName.MasterAdmin);
        await LoginAsync(client, admin);

        using var content = CreateImageContent(1000, 500, "knife.png", "png");
        var token = await GetCsrfTokenAsync(client);
        using var uploadReq = new HttpRequestMessage(HttpMethod.Post, "/api/media") { Content = content };
        uploadReq.Headers.Add("X-CSRF-TOKEN", token);
        var uploadRes = await client.SendAsync(uploadReq);
        var dto = (await uploadRes.Content.ReadFromJsonAsync<MediaResponse>(JsonOptions))!;

        // Anonymous GET of thumbnail
        var thumbRes = await client.GetAsync($"/api/media/{dto.Id}/thumb");
        thumbRes.StatusCode.ShouldBe(HttpStatusCode.OK);
        thumbRes.Content.Headers.ContentType!.MediaType.ShouldBe("image/png");
        thumbRes.Headers.CacheControl!.ToString().ShouldContain("public");
        thumbRes.Headers.CacheControl!.ToString().ShouldContain("immutable");
        thumbRes.Headers.ETag.ShouldNotBeNull();

        // Check thumbnail dimensions
        var thumbBytes = await thumbRes.Content.ReadAsByteArrayAsync();
        using var thumbImg = Image.Load(thumbBytes);
        thumbImg.Width.ShouldBeLessThanOrEqualTo(400);
        thumbImg.Height.ShouldBeLessThanOrEqualTo(400);
    }

    [Fact]
    public async Task Get_missing_media_and_thumbnail_return_404()
    {
        using var factory = new MediaEndpointTestFactory();
        using var client = factory.Browser();

        var originalMissing = await client.GetAsync("/api/media/nonexistent");
        originalMissing.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var thumbMissing = await client.GetAsync("/api/media/nonexistent/thumb");
        thumbMissing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

public sealed class MediaEndpointTestFactory : WebApplicationFactory<Program>
{
    public MemoryUsers Users { get; } = new();
    public MemoryAudit Audit { get; } = new();
    public IntegrationMemoryMediaRepository MediaRepository { get; } = new();
    public IntegrationMemoryFileStore FileStore { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = "mongodb://localhost:27017",
                ["Mongo:Database"] = "media_tests",
                ["Mongo:ApplySchemaOnStartup"] = "false"
            }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<IAuditLogRepository>();
            services.RemoveAll<IMediaAssetRepository>();
            services.RemoveAll<IFileStore>();

            services.AddSingleton<IUserRepository>(Users);
            services.AddSingleton<IAuditLogRepository>(Audit);
            services.AddSingleton<IMediaAssetRepository>(MediaRepository);
            services.AddSingleton<IFileStore>(FileStore);
            services.AddSingleton<IStartupFilter, RemoteAddressFilter>();
        });
    }

    public HttpClient Browser() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });

    public User AddUser(RoleName role = RoleName.Customer)
    {
        var user = new User
        {
            Id = Guid.NewGuid().ToString("N"),
            Email = $"{Guid.NewGuid():N}@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = role,
            UserType = role == RoleName.Customer ? UserType.Client : UserType.Admin,
            PasswordHash = Services.GetRequiredService<IPasswordHasher>().Hash(SecurityTests.Password)
        };
        Users.Items.Add(user);
        return user;
    }
}

public sealed class IntegrationMemoryMediaRepository : IMediaAssetRepository
{
    private readonly Dictionary<string, MediaAsset> _items = new();

    public Task<MediaAsset?> GetAsync(string id, CancellationToken ct = default)
    {
        _items.TryGetValue(id, out var asset);
        return Task.FromResult(asset);
    }

    public Task<string> CreateAsync(MediaAsset asset, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(asset.Id))
        {
            asset.Id = Guid.NewGuid().ToString("N");
        }
        _items[asset.Id] = asset;
        return Task.FromResult(asset.Id);
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        _items.Remove(id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MediaAsset>> FindOrphansAsync(DateTime olderThanUtc, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MediaAsset>>(_items.Values.Where(a => a.UploadedAt < olderThanUtc).ToList());
}

public sealed class IntegrationMemoryFileStore : IFileStore
{
    private readonly Dictionary<string, byte[]> _files = new();

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        var id = Guid.NewGuid().ToString("N");
        _files[id] = ms.ToArray();
        return id;
    }

    public Task<Stream> OpenDownloadAsync(string fileId, CancellationToken ct = default)
    {
        if (!_files.TryGetValue(fileId, out var bytes))
        {
            throw new FileNotFoundException($"File {fileId} not found in store.");
        }
        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    public Task DeleteAsync(string fileId, CancellationToken ct = default)
    {
        _files.Remove(fileId);
        return Task.CompletedTask;
    }
}
