using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Admin;
using SimmerAndSear.Contracts.Engagement;
using SimmerAndSear.Contracts.Paging;

namespace SimmerAndSear.Web.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProductReviewsController(ReviewService reviewService) : ControllerBase
{
    [HttpGet("api/products/{id}/reviews")]
    [AllowAnonymous]
    public async Task<ActionResult<Page<PublicReviewResponse>>> GetPublicReviews(
        string id,
        [Range(1, int.MaxValue)] int pageNumber = 1,
        [Range(1, 100)] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await reviewService.ListPublicForProductAsync(id, pageNumber, pageSize, ct);
        return Ok(result);
    }
}
