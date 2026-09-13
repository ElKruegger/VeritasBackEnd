using LuxuryStore.Application.DTOs;
using LuxuryStore.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LuxuryStore_Backend.Controllers;

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
    [ProducesResponseType<BrandDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BrandDto>> Create(CreateBrandDto dto, CancellationToken cancellationToken)
    {
        var brand = await brandService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = brand.Id }, brand);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<BrandDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrandDto>> Update(Guid id, UpdateBrandDto dto, CancellationToken cancellationToken)
    {
        var brand = await brandService.UpdateAsync(id, dto, cancellationToken);
        return Ok(brand);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await brandService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
