using LuxuryStore.Application.Common;
using LuxuryStore.Application.DTOs;
using LuxuryStore.Application.Exceptions;
using LuxuryStore.Application.Interfaces;
using LuxuryStore.Application.Mapping;
using LuxuryStore.Domain.Entities;

namespace LuxuryStore.Application.Services;

public class ProductService(IUnitOfWork unitOfWork) : IProductService
{
    public async Task<PagedResult<ProductListItemDto>> GetPagedAsync(ProductQueryParams query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await unitOfWork.Products.GetPagedAsync(query, cancellationToken);

        return new PagedResult<ProductListItemDto>
        {
            Items = items.Select(p => p.ToListItemDto()).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Products.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Product '{id}' was not found.");
        return product.ToDto();
    }

    public async Task<ProductDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Products.GetBySlugWithDetailsAsync(slug, cancellationToken)
            ?? throw new NotFoundException($"Product '{slug}' was not found.");
        return product.ToDto();
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        if (await unitOfWork.Products.SlugExistsAsync(dto.Slug, cancellationToken: cancellationToken))
            throw new ConflictException($"A product with slug '{dto.Slug}' already exists.");

        if (await unitOfWork.Brands.GetByIdAsync(dto.BrandId, cancellationToken) is null)
            throw new NotFoundException($"Brand '{dto.BrandId}' was not found.");

        if (await unitOfWork.Categories.GetByIdAsync(dto.CategoryId, cancellationToken) is null)
            throw new NotFoundException($"Category '{dto.CategoryId}' was not found.");

        var product = new Product
        {
            Name = dto.Name,
            Slug = dto.Slug,
            Description = dto.Description,
            BasePrice = dto.BasePrice,
            BrandId = dto.BrandId,
            CategoryId = dto.CategoryId
        };

        await unitOfWork.Products.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await unitOfWork.Products.GetByIdWithDetailsAsync(product.Id, cancellationToken);
        return created!.ToDto();
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        // Fetched without Include() on purpose: only the Product row itself
        // is being changed, so Update() below should only mark that row
        // Modified — not the (unloaded) Brand/Category/Variants graph too.
        var product = await unitOfWork.Products.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Product '{id}' was not found.");

        if (await unitOfWork.Brands.GetByIdAsync(dto.BrandId, cancellationToken) is null)
            throw new NotFoundException($"Brand '{dto.BrandId}' was not found.");

        if (await unitOfWork.Categories.GetByIdAsync(dto.CategoryId, cancellationToken) is null)
            throw new NotFoundException($"Category '{dto.CategoryId}' was not found.");

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.BasePrice = dto.BasePrice;
        product.IsActive = dto.IsActive;
        product.BrandId = dto.BrandId;
        product.CategoryId = dto.CategoryId;
        product.UpdatedAt = DateTime.UtcNow;

        unitOfWork.Products.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await unitOfWork.Products.GetByIdWithDetailsAsync(id, cancellationToken);
        return updated!.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Products.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Product '{id}' was not found.");

        // Variants cascade at the database level (see
        // ProductVariantConfiguration.OnDelete(Cascade)) — no need to load
        // and remove them client-side.
        unitOfWork.Products.Remove(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductVariantDto> AddVariantAsync(Guid productId, CreateProductVariantDto dto, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Products.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException($"Product '{productId}' was not found.");

        var variant = new ProductVariant
        {
            ProductId = product.Id,
            Sku = dto.Sku,
            VariantLabel = dto.VariantLabel,
            PriceOverride = dto.PriceOverride,
            StockQuantity = dto.StockQuantity,
            ImageUrls = dto.ImageUrls
        };

        unitOfWork.Products.AddVariant(variant);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        variant.Product = product; // fixup so ToDto()'s EffectivePrice can read BasePrice without a reload
        return variant.ToDto();
    }

    public async Task<ProductVariantDto> UpdateVariantAsync(Guid productId, Guid variantId, UpdateProductVariantDto dto, CancellationToken cancellationToken = default)
    {
        var variant = await unitOfWork.Products.GetVariantAsync(productId, variantId, cancellationToken)
            ?? throw new NotFoundException($"Variant '{variantId}' was not found on product '{productId}'.");

        variant.PriceOverride = dto.PriceOverride;
        variant.StockQuantity = dto.StockQuantity;
        variant.IsActive = dto.IsActive;
        variant.ImageUrls = dto.ImageUrls;
        variant.UpdatedAt = DateTime.UtcNow;

        unitOfWork.Products.UpdateVariant(variant);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var product = await unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
        variant.Product = product!;
        return variant.ToDto();
    }

    public async Task RemoveVariantAsync(Guid productId, Guid variantId, CancellationToken cancellationToken = default)
    {
        var variant = await unitOfWork.Products.GetVariantAsync(productId, variantId, cancellationToken)
            ?? throw new NotFoundException($"Variant '{variantId}' was not found on product '{productId}'.");

        unitOfWork.Products.RemoveVariant(variant);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
