using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Media;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Web.Security;

namespace SimmerAndSear.Web.Controllers;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Handle requests with controllers in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-10.0> [Accessed 20 September 2026].
//
//Microsoft, 2026. Policy-based authorization in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0> [Accessed 20 September 2026].
//
//Microsoft, 2026. Response caching in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/performance/caching/response?view=aspnetcore-10.0> [Accessed 20 September 2026].
// ────────────────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/media")]
public sealed class MediaController(
    IMediaService mediaService,
    AuditWriter audit,
    CurrentUser current) : ControllerBase
{
    private const string ImmutableCacheControl = "public, max-age=31536000, immutable";

    [HttpPost]
    [Authorize(Policy = nameof(Permission.CatalogueManage))]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "An image file is required.");
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "File exceeds maximum allowed size of 10 MB.");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var asset = await mediaService.UploadAsync(stream, file.FileName, file.Length, current.Id, ct);

            await audit.WriteAsync(
                AuditAction.Create,
                $"Media uploaded: {asset.Id} ({asset.FileName}, {asset.FileSizeBytes} bytes)");

            var response = new MediaResponse(
                asset.Id,
                asset.FileName,
                asset.ContentType,
                asset.FileSizeBytes,
                asset.Width ?? 0,
                asset.Height ?? 0,
                $"/api/media/{asset.Id}",
                $"/api/media/{asset.Id}/thumb",
                asset.UploadedAt);

            return Created($"/api/media/{asset.Id}", response);
        }
        catch (MediaValidationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await mediaService.GetOriginalStreamAsync(id, ct);
        if (result is null)
        {
            return NotFound();
        }

        if (IsCached(result.ETag))
        {
            SetCachingHeaders(result.ETag);
            return StatusCode(StatusCodes.Status304NotModified);
        }

        SetCachingHeaders(result.ETag);
        return File(result.ContentStream, result.ContentType);
    }

    [HttpGet("{id}/thumb")]
    [AllowAnonymous]
    public async Task<IActionResult> GetThumbnail(string id, CancellationToken ct)
    {
        var result = await mediaService.GetThumbnailStreamAsync(id, ct);
        if (result is null)
        {
            return NotFound();
        }

        if (IsCached(result.ETag))
        {
            SetCachingHeaders(result.ETag);
            return StatusCode(StatusCodes.Status304NotModified);
        }

        SetCachingHeaders(result.ETag);
        return File(result.ContentStream, result.ContentType);
    }

    private bool IsCached(string etag)
    {
        if (Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var ifNoneMatch))
        {
            var matchHeader = ifNoneMatch.ToString().Trim();
            if (matchHeader == "*" ||
                string.Equals(matchHeader, etag, StringComparison.Ordinal) ||
                string.Equals(matchHeader.Trim('\"'), etag.Trim('\"'), StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void SetCachingHeaders(string etag)
    {
        Response.Headers[HeaderNames.CacheControl] = ImmutableCacheControl;
        Response.Headers[HeaderNames.ETag] = etag;
    }
}
