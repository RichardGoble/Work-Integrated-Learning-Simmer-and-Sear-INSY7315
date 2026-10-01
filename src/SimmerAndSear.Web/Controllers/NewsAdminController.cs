using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Admin;
using SimmerAndSear.Contracts.Admin;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Web.Security;

namespace SimmerAndSear.Web.Controllers;

[ApiController, Route("api/admin/news")]
[Authorize(Policy = nameof(Permission.CatalogueManage))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class NewsAdminController(NewsService service, CurrentUser current, AuditWriter audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Page<NewsPostResponse>>> List(
        [FromQuery] PostStatus? status,
        [Range(1, int.MaxValue)] int pageNumber = 1,
        [Range(1, 100)] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return Ok(await service.ListAsync(pageNumber, pageSize, status, ct));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<NewsPostResponse>> Get(string id, CancellationToken ct)
    {
        var post = await service.GetAsync(id, ct);
        return post is null ? Problem(statusCode: 404, title: "News post not found.") : Ok(post);
    }

    [HttpPost]
    public async Task<ActionResult<NewsPostResponse>> Create(CreateNewsPostRequest request, CancellationToken ct)
    {
        var post = await service.CreateAsync(request, current.Id!, current.Name, ct);

        await audit.WriteAsync(AuditAction.Create, "NewsPost", post.Id, $"News post created: {post.Title}");

        return Created($"/api/admin/news/{post.Id}", post);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<NewsPostResponse>> Update(string id, UpdateNewsPostRequest request, CancellationToken ct)
    {
        var post = await service.UpdateAsync(id, request, ct);
        if (post is null) return Problem(statusCode: 404, title: "News post not found.");

        await audit.WriteAsync(AuditAction.Update, "NewsPost", id, $"News post updated: {post.Title}");

        return Ok(post);
    }

    [HttpPost("{id}/publish")]
    public async Task<ActionResult<NewsPostResponse>> Publish(string id, CancellationToken ct)
    {
        var (post, error) = await service.PublishAsync(id, ct);

        if (post is null && error is null) return Problem(statusCode: 404, title: "News post not found.");
        if (error is not null) return Problem(statusCode: 400, title: error);

        await audit.WriteAsync(AuditAction.Publish, "NewsPost", id, $"News post published: {post!.Title}");

        return Ok(post);
    }

    [HttpPost("{id}/archive")]
    public async Task<ActionResult<NewsPostResponse>> Archive(string id, CancellationToken ct)
    {
        var post = await service.ArchiveAsync(id, ct);
        if (post is null) return Problem(statusCode: 404, title: "News post not found.");

        await audit.WriteAsync(AuditAction.Update, "NewsPost", id, $"News post archived: {post.Title}");

        return Ok(post);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        if (!deleted) return Problem(statusCode: 404, title: "News post not found.");

        await audit.WriteAsync(AuditAction.Delete, "NewsPost", id, "News post deleted");

        return NoContent();
    }
}
