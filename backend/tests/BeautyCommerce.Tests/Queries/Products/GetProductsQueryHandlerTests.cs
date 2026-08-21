using BeautyCommerce.Application.Features.Products.DTOs;
using BeautyCommerce.Application.Features.Products.Queries.GetProducts;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Tests.Queries.Products;

[Trait("Category", "Integration")]
public class GetProductsQueryHandlerTests : IAsyncLifetime
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=BeautyCommerceDb;Username=postgres;";

    private Guid _brandId;
    private Guid _categoryId;
    private Guid _productId;
    private Guid _variantId;

    private static ApplicationDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(ConnectionString)
                .Options,
            null);

    public async Task InitializeAsync()
    {
        await using var context = NewContext();

        var brand = new Brand { Name = "QA Rating Brand", Description = "QA test brand, safe to delete." };
        var category = new Category { Name = "QA Rating Category", Description = "QA test category, safe to delete." };
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "QA Rated Product",
            Slug = $"qa-rated-{Guid.NewGuid():N}",
            ShortDescription = "QA test product, safe to delete.",
            Description = "Created by GetProductsQueryHandlerTests.",
            BrandId = brand.Id,
            CategoryId = category.Id
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var variant = new ProductVariant
        {
            ProductId = product.Id,
            SKU = $"QA-SKU-{Guid.NewGuid():N}"[..20],
            Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
            Price = 10m,
            Stock = 10
        };
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        _brandId = brand.Id;
        _categoryId = category.Id;
        _productId = product.Id;
        _variantId = variant.Id;
    }

    public async Task DisposeAsync()
    {
        await using var context = NewContext();

        var reviews = await context.Reviews.IgnoreQueryFilters()
            .Where(x => x.ProductId == _productId).ToListAsync();
        context.Reviews.RemoveRange(reviews);
        await context.SaveChangesAsync();

        var variant = await context.ProductVariants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == _variantId);
        if (variant != null) context.ProductVariants.Remove(variant);
        await context.SaveChangesAsync();

        var product = await context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == _productId);
        if (product != null) context.Products.Remove(product);
        await context.SaveChangesAsync();

        var category = await context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == _categoryId);
        if (category != null) context.Categories.Remove(category);

        var brand = await context.Brands.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == _brandId);
        if (brand != null) context.Brands.Remove(brand);

        await context.SaveChangesAsync();
    }

    private async Task<ProductListItemDto> GetSeededProductAsync(ApplicationDbContext context)
    {
        var handler = new GetProductsQueryHandler(context);

        var result = await handler.Handle(
            new GetProductsQuery { Filter = new ProductFilterDto { Search = "QA Rated Product" } },
            default);

        return result.Items.Should().ContainSingle().Subject;
    }

    [Fact]
    public async Task Product_Without_Reviews_Reports_Null_Average_And_Zero_Count()
    {
        await using var context = NewContext();

        var item = await GetSeededProductAsync(context);

        item.AverageRating.Should().BeNull("a product with no reviews has no average to report");
        item.ReviewCount.Should().Be(0);
    }

    [Fact]
    public async Task Product_With_Reviews_Reports_Correct_Average_And_Count()
    {
        await using var context = NewContext();

        foreach (var rating in new[] { 5, 4, 3 })
        {
            context.Reviews.Add(new Review
            {
                UserId = Guid.NewGuid(),
                ProductId = _productId,
                Rating = rating,
                Comment = "QA review"
            });
        }
        await context.SaveChangesAsync();

        var item = await GetSeededProductAsync(context);

        item.AverageRating.Should().Be(4m, "(5 + 4 + 3) / 3 = 4");
        item.ReviewCount.Should().Be(3);
    }

    [Fact]
    public async Task Search_Matches_Regardless_Of_Case()
    {
        await using var context = NewContext();

        var handler = new GetProductsQueryHandler(context);

        var result = await handler.Handle(
            new GetProductsQuery { Filter = new ProductFilterDto { Search = "qa rated product" } },
            default);

        result.Items.Should().ContainSingle(
            x => x.Id == _productId,
            "PostgreSQL's default LIKE is case-sensitive, so the search must use ILIKE");
    }
}
