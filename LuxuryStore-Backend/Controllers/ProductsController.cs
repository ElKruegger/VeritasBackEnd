using LuxuryStore.Application.Common;
using LuxuryStore.Application.DTOs;
using LuxuryStore.Application.Interfaces;
using LuxuryStore_Backend.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuxuryStore_Backend.Controllers;

/// <summary>
/// Reads are public (the storefront). Writes are AdminOnly: the brief keeps
/// catalog management in Supabase Studio for v1, but these endpoints stay as
/// the API-side path for a future admin panel and for scripted maintenance —
/// they were previously open to anyone on the internet.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProductListItemDto>>> GetPaged(
        [FromQuery] ProductQueryParams query, CancellationToken cancellationToken)
    {
        var result = await productService.GetPagedAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Filter sidebar of a catalog page: brands, sizes and price range that
    /// actually exist for the given category/brand/search.
    /// </summary>
    [HttpGet("filters")]
    [ProducesResponseType<ProductFiltersDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductFiltersDto>> GetFilters(
        [FromQuery] ProductFilterQuery query, CancellationToken cancellationToken)
    {
        return Ok(await productService.GetFiltersAsync(query, cancellationToken));
    }

    /// <summary>"You may also like": same category, same brand first.</summary>
    [HttpGet("{id:guid}/related")]
    [ProducesResponseType<IReadOnlyList<ProductListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ProductListItemDto>>> GetRelated(
        Guid id, [FromQuery] RelatedProductsQuery query, CancellationToken cancellationToken)
    {
        return Ok(await productService.GetRelatedAsync(id, query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await productService.GetByIdAsync(id, cancellationToken);
        return Ok(product);
    }

    [HttpGet("slug/{slug}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var product = await productService.GetBySlugAsync(slug, cancellationToken);
        return Ok(product);
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Create(CreateProductDto dto, CancellationToken cancellationToken)
    {
        var product = await productService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(Guid id, UpdateProductDto dto, CancellationToken cancellationToken)
    {
        var product = await productService.UpdateAsync(id, dto, cancellationToken);
        return Ok(product);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await productService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    // --- Variants: a nested resource, since a variant never exists apart from its product ---

    [HttpPost("{productId:guid}/variants")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProductVariantDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductVariantDto>> AddVariant(
        Guid productId, CreateProductVariantDto dto, CancellationToken cancellationToken)
    {
        var variant = await productService.AddVariantAsync(productId, dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = productId }, variant);
    }

    [HttpPut("{productId:guid}/variants/{variantId:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProductVariantDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductVariantDto>> UpdateVariant(
        Guid productId, Guid variantId, UpdateProductVariantDto dto, CancellationToken cancellationToken)
    {
        var variant = await productService.UpdateVariantAsync(productId, variantId, dto, cancellationToken);
        return Ok(variant);
    }

    [HttpDelete("{productId:guid}/variants/{variantId:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveVariant(Guid productId, Guid variantId, CancellationToken cancellationToken)
    {
        await productService.RemoveVariantAsync(productId, variantId, cancellationToken);
        return NoContent();
    }

    // --- Images: metadata only; the file itself lives in Supabase Storage ---

    [HttpPost("{productId:guid}/images")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProductImageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductImageDto>> AddImage(
        Guid productId, CreateProductImageDto dto, CancellationToken cancellationToken)
    {
        var image = await productService.AddImageAsync(productId, dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = productId }, image);
    }

    [HttpDelete("{productId:guid}/images/{imageId:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveImage(Guid productId, Guid imageId, CancellationToken cancellationToken)
    {
        await productService.RemoveImageAsync(productId, imageId, cancellationToken);
        return NoContent();
    }
}
