using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Wishlist.Queries.GetWishlist;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace BeautyCommerce.Tests.Integrity;

public class GetWishlistPriceAndImageTests
{
    [Fact]
    public async Task Returns_The_Real_Minimum_Variant_Price_And_The_Real_Image_Url()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var brand = new Brand { Name = "QA Brand", Description = "QA" };
        var category = new Category { Name = "QA Category", Description = "QA" };
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "QA Wishlist Product",
            Slug = $"qa-wishlist-{Guid.NewGuid():N}",
            BrandId = brand.Id,
            CategoryId = category.Id
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        const decimal expensiveVariantPrice = 99999.99m;
        const decimal cheapestVariantPrice = 12345.67m;
        const string expectedImageUrl = "https://example.com/qa-wishlist-image.jpg";

        context.ProductVariants.AddRange(
            new ProductVariant
            {
                ProductId = product.Id,
                SKU = $"QA-EXP-{Guid.NewGuid():N}"[..20],
                Barcode = $"QA-EXP-{Guid.NewGuid():N}"[..20],
                Price = expensiveVariantPrice,
                Stock = 5
            },
            new ProductVariant
            {
                ProductId = product.Id,
                SKU = $"QA-CHP-{Guid.NewGuid():N}"[..20],
                Barcode = $"QA-CHP-{Guid.NewGuid():N}"[..20],
                Price = cheapestVariantPrice,
                Stock = 5
            });

        context.ProductImages.Add(new ProductImage
        {
            ProductId = product.Id,
            ImageUrl = expectedImageUrl,
            IsPrimary = true
        });

        await context.SaveChangesAsync();

        var userId = Guid.NewGuid();
        context.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = product.Id });
        await context.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);

        var handler = new GetWishlistQueryHandler(context, currentUser.Object);
        var result = await handler.Handle(new GetWishlistQuery(), default);

        var item = result.Should().ContainSingle().Which;

        item.ProductId.Should().Be(product.Id);
        item.ProductName.Should().Be("QA Wishlist Product");
        item.Price.Should().Be(cheapestVariantPrice,
            "the price policy is the minimum across variants, and it must now actually see the loaded variants");
        item.ImageUrl.Should().Be(expectedImageUrl);
    }
}
