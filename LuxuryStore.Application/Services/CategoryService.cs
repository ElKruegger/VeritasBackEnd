using LuxuryStore.Application.DTOs;
using LuxuryStore.Application.Exceptions;
using LuxuryStore.Application.Interfaces;
using LuxuryStore.Application.Mapping;
using LuxuryStore.Domain.Entities;

namespace LuxuryStore.Application.Services;

public class CategoryService(IUnitOfWork unitOfWork) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await unitOfWork.Categories.GetAllAsync(cancellationToken);
        return categories.Select(c => c.ToDto()).ToList();
    }

    public async Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await unitOfWork.Categories.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Category '{id}' was not found.");
        return category.ToDto();
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        if (await unitOfWork.Categories.SlugExistsAsync(dto.Slug, cancellationToken: cancellationToken))
            throw new ConflictException($"A category with slug '{dto.Slug}' already exists.");

        var category = new Category
        {
            Name = dto.Name,
            Slug = dto.Slug,
            Description = dto.Description
        };

        await unitOfWork.Categories.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return category.ToDto();
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var category = await unitOfWork.Categories.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Category '{id}' was not found.");

        category.Name = dto.Name;
        category.Description = dto.Description;
        category.IsActive = dto.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        unitOfWork.Categories.Update(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return category.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await unitOfWork.Categories.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Category '{id}' was not found.");

        unitOfWork.Categories.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
