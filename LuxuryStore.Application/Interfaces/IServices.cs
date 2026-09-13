using LuxuryStore.Application.Common;
using LuxuryStore.Application.DTOs;

namespace LuxuryStore.Application.Interfaces;

// These are the abstractions controllers depend on (constructor injection)
// instead of depending on BrandService/CategoryService/ProductService
// directly. A controller that only knows "something implementing
// IBrandService" can be unit-tested with a hand-written fake, and the real
// implementation can be swapped without touching the controller.

public interface IBrandService
{
    Task<IReadOnlyList<BrandDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BrandDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BrandDto> CreateAsync(CreateBrandDto dto, CancellationToken cancellationToken = default);
    Task<BrandDto> UpdateAsync(Guid id, UpdateBrandDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IProductService
{
    Task<PagedResult<ProductListItemDto>> GetPagedAsync(ProductQueryParams query, CancellationToken cancellationToken = default);
    Task<ProductDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProductVariantDto> AddVariantAsync(Guid productId, CreateProductVariantDto dto, CancellationToken cancellationToken = default);
    Task<ProductVariantDto> UpdateVariantAsync(Guid productId, Guid variantId, UpdateProductVariantDto dto, CancellationToken cancellationToken = default);
    Task RemoveVariantAsync(Guid productId, Guid variantId, CancellationToken cancellationToken = default);
}
