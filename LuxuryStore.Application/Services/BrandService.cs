using LuxuryStore.Application.DTOs;
using LuxuryStore.Application.Exceptions;
using LuxuryStore.Application.Interfaces;
using LuxuryStore.Application.Mapping;
using LuxuryStore.Domain.Entities;

namespace LuxuryStore.Application.Services;

/// <summary>
/// Application-layer business logic for brands. Depends only on
/// IUnitOfWork — an abstraction — never on EF Core or Npgsql directly.
/// That's what lets this class be unit-tested with a fake repository
/// instead of a real Postgres database, and what keeps this project free of
/// any Infrastructure package reference.
///
/// Constructor injection via a primary constructor: the DI container
/// resolves an IUnitOfWork (registered as Scoped in
/// LuxuryStore.Infrastructure/DependencyInjection.cs) and passes it in —
/// this class never calls `new UnitOfWork(...)` itself.
/// </summary>
public class BrandService(IUnitOfWork unitOfWork) : IBrandService
{
    public async Task<IReadOnlyList<BrandDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var brands = await unitOfWork.Brands.GetAllAsync(cancellationToken);
        return brands.Select(b => b.ToDto()).ToList();
    }

    public async Task<BrandDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var brand = await unitOfWork.Brands.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Brand '{id}' was not found.");
        return brand.ToDto();
    }

    public async Task<BrandDto> CreateAsync(CreateBrandDto dto, CancellationToken cancellationToken = default)
    {
        if (await unitOfWork.Brands.SlugExistsAsync(dto.Slug, cancellationToken: cancellationToken))
            throw new ConflictException($"A brand with slug '{dto.Slug}' already exists.");

        var brand = new Brand
        {
            Name = dto.Name,
            Slug = dto.Slug,
            LogoUrl = dto.LogoUrl
        };

        await unitOfWork.Brands.AddAsync(brand, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return brand.ToDto();
    }

    public async Task<BrandDto> UpdateAsync(Guid id, UpdateBrandDto dto, CancellationToken cancellationToken = default)
    {
        var brand = await unitOfWork.Brands.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Brand '{id}' was not found.");

        brand.Name = dto.Name;
        brand.LogoUrl = dto.LogoUrl;
        brand.IsActive = dto.IsActive;
        brand.UpdatedAt = DateTime.UtcNow;

        unitOfWork.Brands.Update(brand);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return brand.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var brand = await unitOfWork.Brands.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Brand '{id}' was not found.");

        unitOfWork.Brands.Remove(brand);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
