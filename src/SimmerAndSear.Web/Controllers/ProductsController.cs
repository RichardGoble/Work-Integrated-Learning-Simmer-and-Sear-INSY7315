using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Catalogue;
using SimmerAndSear.Contracts.Catalogue;
using SimmerAndSear.Contracts.Paging;
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
//Microsoft, 2026. Handle errors in ASP.NET Core controller-based web APIs [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-10.0> [Accessed 20 September 2026].
// ────────────────────────────────────────────────────────────────────────────

[ApiController, Route("api/products")]
public sealed class ProductsController(
    CatalogueService catalogue, CurrentUser current, AuditWriter audit) : ControllerBase
{
    [HttpGet, AllowAnonymous]
    public async Task<ActionResult<Page<ProductSummaryResponse>>> Search(
        [FromQuery] ProductFilter filter, CancellationToken ct) =>
        Ok(await catalogue.SearchAsync(filter, CanSeeDrafts, ct));

    [HttpGet("facets"), AllowAnonymous]
    public async Task<ActionResult<CatalogueFacets>> Facets(CancellationToken ct) =>
        Ok(await catalogue.GetFacetsAsync(CanSeeDrafts, ct));

    [HttpGet("{id}"), AllowAnonymous]
    public async Task<ActionResult<ProductDetailResponse>> Get(string id, CancellationToken ct)
    {
        var product = await catalogue.GetAsync(id, CanSeeDrafts, ct);

        return product is null
            ? Problem(statusCode: 404, title: "That product could not be found.")
            : Ok(product);
    }

    [HttpPost]
    [Authorize(Policy = nameof(Permission.CatalogueManage))]
    public async Task<ActionResult<ProductDetailResponse>> Create(
        CreateProductRequest request, CancellationToken ct)
    {
        try
        {
            var product = await catalogue.CreateAsync(request, ct);

            await audit.WriteAsync(AuditAction.Create,
                $"Product {product.Sku} created: {product.Name}");

            return Created($"/api/products/{product.Id}", product);
        }
        catch (ProductRuleException ex)
        {
            return Problem(statusCode: 400, title: ex.Message);
        }
        catch (DuplicateSkuException ex)
        {
            return Problem(statusCode: 409, title: ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = nameof(Permission.CatalogueManage))]
    public async Task<ActionResult<ProductDetailResponse>> Update(
        string id, UpdateProductRequest request, CancellationToken ct)
    {
        try
        {
            var product = await catalogue.UpdateAsync(id, request, ct);

            if (product is null)
            {
                return Problem(statusCode: 404, title: "That product could not be found.");
            }

            await audit.WriteAsync(AuditAction.Update,
                $"Product {product.Sku} updated: {product.Name}");

            return Ok(product);
        }
        catch (ProductRuleException ex)
        {
            return Problem(statusCode: 400, title: ex.Message);
        }
        catch (DuplicateSkuException ex)
        {
            return Problem(statusCode: 409, title: ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = nameof(Permission.CatalogueManage))]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var product = await catalogue.DeleteAsync(id, ct);

        if (product is null)
        {
            return Problem(statusCode: 404, title: "That product could not be found.");
        }

        await audit.WriteAsync(AuditAction.Delete,
            $"Product {product.Sku} deleted: {product.Name}");

        return NoContent();
    }

    private bool CanSeeDrafts =>
        current.Role is RoleName.MasterAdmin or RoleName.SalesAdmin;
}
