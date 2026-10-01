using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Engagement;
using SimmerAndSear.Contracts.Engagement;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Web.Security;

namespace SimmerAndSear.Web.Controllers;

[ApiController]
public sealed class BookingsController(
    BookingService bookingService,
    CurrentUser current,
    AuditWriter audit) : Controller
{
    [HttpPost("api/bookings")]
    [AllowAnonymous]
    public async Task<ActionResult<BookingResponse>> CreateBooking(
        [FromBody] CreateBookingRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var isAuthenticated = current.Id is not null;
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        var (response, error) = await bookingService.CreateAsync(
            request,
            isAuthenticated ? current.Id : null,
            isAuthenticated ? current.Name : null,
            isAuthenticated ? userEmail : null,
            ct);

        if (error is not null)
        {
            return Problem(statusCode: 400, title: error);
        }

        if (isAuthenticated)
        {
            await audit.WriteAsync(
                AuditAction.Create,
                "Booking",
                response!.Id,
                $"Consultation booking created for order {response.OrderId ?? "none"}");
        }

        return Created($"/api/bookings/{response!.Id}", response);
    }

    [HttpGet("api/admin/bookings")]
    [Authorize(Policy = nameof(Permission.InquiryManage))]
    public async Task<ActionResult<List<BookingResponse>>> ListBookings(CancellationToken ct)
    {
        var items = await bookingService.ListAsync(ct);
        return Ok(items);
    }

    [HttpGet("/Bookings")]
    [AllowAnonymous]
    public IActionResult Index([FromQuery] string? orderId)
    {
        ViewData["Title"] = "Book a Consultation";
        ViewData["OrderId"] = orderId;
        return View();
    }
}
