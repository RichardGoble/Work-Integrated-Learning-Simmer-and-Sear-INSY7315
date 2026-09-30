using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Admin;

namespace SimmerAndSear.Web.Controllers;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Handle requests with controllers in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Views in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────


public class NewsController(NewsService news) : Controller
{
    private const int PageSize = 10;

    [Route("/news")]
    public async Task<IActionResult> Index(int pageNumber = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Journal";

        return View(await news.ListPublishedAsync(pageNumber < 1 ? 1 : pageNumber, PageSize, ct));
    }

    [Route("/news/{id}")]
    public async Task<IActionResult> Detail(string id, CancellationToken ct = default)
    {
        var post = await news.GetPublishedAsync(id, ct);

        if (post is null)
        {
            return NotFound();
        }

        ViewData["Title"] = post.Title;
        return View(post);
    }
}
