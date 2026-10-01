using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SimmerAndSear.Application.Engagement;
using SimmerAndSear.Contracts.Engagement;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Web.Security;

namespace SimmerAndSear.Web.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ContactController(
    ContactService contactService,
    CurrentUser current,
    AuditWriter audit) : ControllerBase
{
    [HttpPost("api/contact")]
    [AllowAnonymous]
    [EnableRateLimiting(SecurityHardening.AccountPolicy)]
    public async Task<ActionResult<ContactResponse>> SubmitContact(
        [FromBody] CreateContactRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await contactService.SubmitAsync(request, ct);
        return Created($"/api/contact/{response.Id}", response);
    }

    [HttpGet("api/admin/contact")]
    [Authorize(Policy = nameof(Permission.InquiryManage))]
    public async Task<ActionResult<List<ContactResponse>>> ListContacts(CancellationToken ct)
    {
        var items = await contactService.ListAsync(ct);
        return Ok(items);
    }

    [HttpPut("api/admin/contact/{id}")]
    [Authorize(Policy = nameof(Permission.InquiryManage))]
    public async Task<ActionResult<ContactResponse>> UpdateContactStatus(
        string id,
        [FromBody] UpdateContactStatusRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!Enum.TryParse<ContactStatus>(request.Status, true, out var newStatus))
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Invalid contact status value."
            });
        }

        var staffId = current.Id ?? "staff";
        var updated = await contactService.UpdateStatusAsync(id, newStatus, staffId, ct);

        if (updated is null)
        {
            return Problem(statusCode: 404, title: "Contact request not found.");
        }

        await audit.WriteAsync(
            AuditAction.Update,
            "ContactRequest",
            id,
            $"Contact request status updated to {newStatus} by {staffId}");

        return Ok(updated);
    }
}
