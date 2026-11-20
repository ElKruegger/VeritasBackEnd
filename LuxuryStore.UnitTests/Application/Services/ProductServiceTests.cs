using LuxuryStore.Application.DTOs;
using LuxuryStore.Application.Exceptions;
using LuxuryStore.Application.Interfaces;
using LuxuryStore.Application.Services;
using LuxuryStore.Domain.Entities;
using static LuxuryStore.UnitTests.TestHelpers;

namespace LuxuryStore.UnitTests.Application.Services;

public class ProductServiceTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IBrandRepository _brands = Substitute.For<IBrandRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly IImageUrlResolver _imageUrls = Substitute.For<IImageUrlResolver>();
    private readonly ProductService _sut;

    private readonly Product _product = new() { Name = "Submariner Date 41 mm", Slug = "rolex-submariner-date", BasePrice = 10950m };

    public ProductServiceTests()
    {
        _unitOfWork.Products.Returns(_products);
        _unitOfWork.Brands.Returns(_brands);
        _unitOfWork.Categories.Returns(_categories);
        _imageUrls.Resolve(Arg.Any<string>(), Arg.Any<string>()).Returns(call => "resolved:" + call.ArgAt<string>(0));
        _products.GetByIdAsync(_product.Id, Arg.Any<CancellationToken>()).Returns(_product);

        _sut = new ProductService(_unitOfWork, _imageUrls);
    }

    // --- Create ---------------------------------------------------------

    [Fact]
    public async Task Create_with_a_duplicate_slug_throws_conflict()
    {
        _products.SlugExistsAsync("rolex-submariner-date", Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);
        var dto = new CreateProductDto { Name = "X", Slug = "rolex-submariner-date", BasePrice = 1, BrandId = Guid.NewGuid(), CategoryId = Guid.NewGuid() };

        await Should.ThrowAsync<ConflictException>(() => _sut.CreateAsync(dto, Ct));
        await _products.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_with_an_unknown_brand_throws_not_found()
    {
        var dto = new CreateProductDto { Name = "X", Slug = "new-product", BasePrice = 1, BrandId = Guid.NewGuid(), CategoryId = Guid.NewGuid() };

        await Should.ThrowAsync<NotFoundException>(() => _sut.CreateAsync(dto, Ct));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // --- Images ---------------------------------------------------------

    [Fact]
    public async Task Adding_an_image_to_a_missing_product_throws_not_found()
    {
        var dto = new CreateProductImageDto { Path = "products/x/01.jpg", AltText = "Alt" };

        await Should.ThrowAsync<NotFoundException>(() => _sut.AddImageAsync(Guid.NewGuid(), dto, Ct));
        _products.DidNotReceive().AddImage(Arg.Any<ProductImage>());
    }

    [Fact]
    public async Task Adding_an_image_for_a_variant_of_another_product_throws_not_found()
    {
        // GetVariantAsync is scoped to (productId, variantId): a variant id
        // that belongs to a different product resolves to null here.
        var dto = new CreateProductImageDto { Path = "products/x/01.jpg", AltText = "Alt", VariantId = Guid.NewGuid() };

        await Should.ThrowAsync<NotFoundException>(() => _sut.AddImageAsync(_product.Id, dto, Ct));
        _products.DidNotReceive().AddImage(Arg.Any<ProductImage>());
    }

    [Fact]
    public async Task First_image_of_a_product_gets_sort_order_zero()
    {
        _products.GetMaxImageSortOrderAsync(_product.Id, Arg.Any<CancellationToken>()).Returns((int?)null);
        var dto = new CreateProductImageDto { Path = "products/rolex/01.jpg", AltText = "Front" };

        var result = await _sut.AddImageAsync(_product.Id, dto, Ct);

        result.SortOrder.ShouldBe(0);
    }

    [Fact]
    public async Task New_image_without_sort_order_is_appended_after_the_last_one()
    {
        _products.GetMaxImageSortOrderAsync(_product.Id, Arg.Any<CancellationToken>()).Returns(2);
        var dto = new CreateProductImageDto { Path = "products/rolex/04.jpg", AltText = "Lifestyle" };

        var result = await _sut.AddImageAsync(_product.Id, dto, Ct);

        result.SortOrder.ShouldBe(3);
    }

    [Fact]
    public async Task Explicit_sort_order_is_kept_and_input_is_trimmed()
    {
        var dto = new CreateProductImageDto { Path = "  products/rolex/01.jpg ", AltText = " Front view ", SortOrder = 7 };

        var result = await _sut.AddImageAsync(_product.Id, dto, Ct);

        result.SortOrder.ShouldBe(7);
        result.AltText.ShouldBe("Front view");
        result.Url.ShouldBe("resolved:products/rolex/01.jpg");
        _products.Received(1).AddImage(Arg.Is<ProductImage>(i => i.Path == "products/rolex/01.jpg" && i.ProductId == _product.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Removing_an_image_that_does_not_belong_to_the_product_throws_not_found()
    {
        await Should.ThrowAsync<NotFoundException>(() => _sut.RemoveImageAsync(_product.Id, Guid.NewGuid(), Ct));
        _products.DidNotReceive().RemoveImage(Arg.Any<ProductImage>());
    }

    // --- Related products -------------------------------------------------

    [Fact]
    public async Task Related_products_of_an_unknown_product_throws_not_found()
    {
        await Should.ThrowAsync<NotFoundException>(() =>
            _sut.GetRelatedAsync(Guid.NewGuid(), new RelatedProductsQuery(), Ct));
    }

    [Fact]
    public async Task Related_products_of_an_inactive_product_throws_not_found()
    {
        _product.IsActive = false;

        await Should.ThrowAsync<NotFoundException>(() =>
            _sut.GetRelatedAsync(_product.Id, new RelatedProductsQuery(), Ct));
        await _products.DidNotReceive().GetRelatedAsync(Arg.Any<Product>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Related_products_are_requested_with_the_given_limit_and_mapped_to_listing_items()
    {
        var related = new Product
        {
            Name = "Explorer 40 mm",
            Slug = "rolex-explorer",
            Brand = new Brand { Name = "Rolex" },
            Category = new Category { Name = "Watches" }
        };
        _products.GetRelatedAsync(_product, 6, Arg.Any<CancellationToken>()).Returns([related]);

        var result = await _sut.GetRelatedAsync(_product.Id, new RelatedProductsQuery { Limit = 6 }, Ct);

        result.Single().Slug.ShouldBe("rolex-explorer");
        result.Single().BrandName.ShouldBe("Rolex");
    }

    // --- Filters ----------------------------------------------------------

    [Fact]
    public async Task Filter_sizes_come_back_in_natural_order()
    {
        var query = new ProductFilterQuery();
        _products.GetFacetsAsync(query, Arg.Any<CancellationToken>())
            .Returns(new ProductFacets([], ["UK 10", "UK 7", "UK 9", "UK 8"], 650m, 1890m));

        var result = await _sut.GetFiltersAsync(query, Ct);

        result.Sizes.ShouldBe(["UK 7", "UK 8", "UK 9", "UK 10"]);
        result.MinPrice.ShouldBe(650m);
        result.MaxPrice.ShouldBe(1890m);
    }

    [Fact]
    public async Task Filter_brands_are_mapped_to_dtos()
    {
        var query = new ProductFilterQuery();
        var brand = new Brand { Name = "Crockett & Jones", Slug = "crockett-jones", IsActive = true };
        _products.GetFacetsAsync(query, Arg.Any<CancellationToken>())
            .Returns(new ProductFacets([brand], [], null, null));

        var result = await _sut.GetFiltersAsync(query, Ct);

        result.Brands.Single().Slug.ShouldBe("crockett-jones");
        result.MinPrice.ShouldBeNull();
    }
}
