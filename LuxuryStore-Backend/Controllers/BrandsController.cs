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
public class BrandsController(IBrandService brandService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BrandDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BrandDto>>> GetAll(CancellationToken cancellationToken)
    {
        var brands = await brandService.GetAllAsync(cancellationToken);
        return Ok(brands);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<BrandDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrandDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var brand = await brandService.GetByIdAsync(id, cancellationToken);
        return Ok(brand);
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<BrandDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BrandDto>> Create(CreateBrandDto dto, CancellationToken cancellationToken)
    {
        var brand = await brandService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = brand.Id }, brand);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<BrandDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrandDto>> Update(Guid id, UpdateBrandDto dto, CancellationToken cancellationToken)
    {
        var brand = await brandService.UpdateAsync(id, dto, cancellationToken);
        return Ok(brand);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await brandService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
