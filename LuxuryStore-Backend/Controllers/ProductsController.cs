using LuxuryStore.Application.Common;
using LuxuryStore.Application.DTOs;
using LuxuryStore.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LuxuryStore_Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductListItemDto>>> GetPaged(
        [FromQuery] ProductQueryParams query, CancellationToken cancellationToken)
    {
        var result = await productService.GetPagedAsync(query, cancellationToken);
        return Ok(result);
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
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Create(CreateProductDto dto, CancellationToken cancellationToken)
    {
        var product = await productService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(Guid id, UpdateProductDto dto, CancellationToken cancellationToken)
    {
        var product = await productService.UpdateAsync(id, dto, cancellationToken);
        return Ok(product);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await productService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    // --- Variants: a nested resource, since a variant never exists apart from its product ---

    [HttpPost("{productId:guid}/variants")]
    [ProducesResponseType<ProductVariantDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductVariantDto>> AddVariant(
        Guid productId, CreateProductVariantDto dto, CancellationToken cancellationToken)
    {
        var variant = await productService.AddVariantAsync(productId, dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = productId }, variant);
    }

    [HttpPut("{productId:guid}/variants/{variantId:guid}")]
    [ProducesResponseType<ProductVariantDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductVariantDto>> UpdateVariant(
        Guid productId, Guid variantId, UpdateProductVariantDto dto, CancellationToken cancellationToken)
    {
        var variant = await productService.UpdateVariantAsync(productId, variantId, dto, cancellationToken);
        return Ok(variant);
    }

    [HttpDelete("{productId:guid}/variants/{variantId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveVariant(Guid productId, Guid variantId, CancellationToken cancellationToken)
    {
        await productService.RemoveVariantAsync(productId, variantId, cancellationToken);
        return NoContent();
    }
}
