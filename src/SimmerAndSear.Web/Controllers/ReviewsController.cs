using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Admin;
using SimmerAndSear.Contracts.Admin;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Web.Security;

namespace SimmerAndSear.Web.Controllers;

[ApiController, Route("api/reviews")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ReviewsController(ReviewService service, CurrentUser current, AuditWriter audit) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = nameof(Permission.ReviewManage))]
    public async Task<ActionResult<Page<ReviewResponse>>> List(
        [FromQuery] ReviewStatus? status,
        [Range(1, int.MaxValue)] int pageNumber = 1,
        [Range(1, 100)] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return Ok(await service.ListAsync(pageNumber, pageSize, status, ct));
    }

    [HttpGet("{id}")]
    [Authorize(Policy = nameof(Permission.ReviewManage))]
    public async Task<ActionResult<ReviewResponse>> Get(string id, CancellationToken ct)
    {
        var review = await service.GetAsync(id, ct);
        return review is null ? Problem(statusCode: 404, title: "Review not found.") : Ok(review);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Customer)]
    public async Task<ActionResult<ReviewResponse>> Submit(SubmitReviewRequest request, CancellationToken ct)
    {
        var (review, error) = await service.SubmitAsync(current.Id!, current.Name, request, ct);

        if (review is null && error is null)
            return Problem(statusCode: 404, title: "Product not found.");

        if (error is not null)
            return Problem(statusCode: 400, title: error);

        await audit.WriteAsync(AuditAction.Create, "Review", review!.Id,
            $"Review submitted for product {review.ProductId}");

        return Created($"/api/reviews/{review.Id}", review);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Permissions.Customer)]
    public async Task<ActionResult<ReviewResponse>> Edit(string id, EditReviewRequest request, CancellationToken ct)
    {
        var (review, error) = await service.EditAsync(id, current.Id!, request, ct);

        if (review is null && error is null)
            return Problem(statusCode: 404, title: "Review not found.");

        if (error == "Forbidden")
            return Problem(statusCode: 403, title: "You can only edit your own reviews.");

        await audit.WriteAsync(AuditAction.Update, "Review", review!.Id,
            $"Review {id} edited by customer; returned to Pending if previously Approved");

        return Ok(review);
    }

    [HttpPost("{id}/approve")]
    [Authorize(Policy = nameof(Permission.ReviewManage))]
    public async Task<ActionResult<ReviewResponse>> Approve(string id, CancellationToken ct)
    {
        var review = await service.ApproveAsync(id, current.Id!, ct);
        if (review is null) return Problem(statusCode: 404, title: "Review not found.");

        await audit.WriteAsync(AuditAction.Update, "Review", id,
            $"Review approved for product {review.ProductId}");

        return Ok(review);
    }

    [HttpPost("{id}/reject")]
    [Authorize(Policy = nameof(Permission.ReviewManage))]
    public async Task<ActionResult<ReviewResponse>> Reject(string id, CancellationToken ct)
    {
        var review = await service.RejectAsync(id, current.Id!, ct);
        if (review is null) return Problem(statusCode: 404, title: "Review not found.");

        await audit.WriteAsync(AuditAction.Update, "Review", id,
            $"Review rejected for product {review.ProductId}");

        return Ok(review);
    }
}
