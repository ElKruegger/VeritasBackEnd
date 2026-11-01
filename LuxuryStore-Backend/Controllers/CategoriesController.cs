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
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetAllAsync(cancellationToken);
        return Ok(categories);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var category = await categoryService.GetByIdAsync(id, cancellationToken);
        return Ok(category);
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryDto dto, CancellationToken cancellationToken)
    {
        var category = await categoryService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> Update(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken)
    {
        var category = await categoryService.UpdateAsync(id, dto, cancellationToken);
        return Ok(category);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await categoryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
