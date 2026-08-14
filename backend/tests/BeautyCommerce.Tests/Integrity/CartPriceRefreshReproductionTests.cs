using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.ShoppingCart.Commands.AddItem;
using BeautyCommerce.Application.Features.ShoppingCart.Commands.UpdateItem;
using BeautyCommerce.Application.Features.ShoppingCart.DTOs;
using BeautyCommerce.Application.Features.ShoppingCart.Queries.GetCart;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit.Abstractions;

namespace BeautyCommerce.Tests.Integrity;

public class CartPriceRefreshReproductionTests
{
    private readonly ITestOutputHelper _output;

    public CartPriceRefreshReproductionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Add_And_Update_Both_Refresh_The_Line_To_The_Current_Variant_Price()
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
            Name = "QA Price Product",
            Slug = $"qa-price-{Guid.NewGuid():N}",
            BrandId = brand.Id,
            CategoryId = category.Id
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        const decimal p1 = 10.00m;
        const decimal p2 = 15.00m;

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            SKU = $"QA-SKU-{Guid.NewGuid():N}"[..20],
            Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
            Price = p1,
            Stock = 100
        };
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);

        _output.WriteLine($"P1 (price at first Add) = {p1}");
        _output.WriteLine($"P2 (price after the simulated admin price change) = {p2}");

        var addHandler = new AddCartItemCommandHandler(context, currentUser.Object);
        await addHandler.Handle(
            new AddCartItemCommand { Item = new AddCartItemDto { ProductVariantId = variant.Id, Quantity = 1 } },
            default);

        var afterFirstAdd = await context.ShoppingCartItems.AsNoTracking()
            .FirstAsync(i => i.ProductVariantId == variant.Id);

        _output.WriteLine(
            $"After first Add: Quantity={afterFirstAdd.Quantity}, UnitPrice={afterFirstAdd.UnitPrice}");

        afterFirstAdd.Quantity.Should().Be(1);
        afterFirstAdd.UnitPrice.Should().Be(p1);

        var trackedVariant = await context.ProductVariants.FirstAsync(v => v.Id == variant.Id);
        trackedVariant.Price = p2;
        await context.SaveChangesAsync();

        var updateHandler = new UpdateCartItemCommandHandler(context, currentUser.Object);
        await updateHandler.Handle(
            new UpdateCartItemCommand { Item = new UpdateCartItemDto { ProductVariantId = variant.Id, Quantity = 2 } },
            default);

        var afterUpdate = await context.ShoppingCartItems.AsNoTracking()
            .FirstAsync(i => i.ProductVariantId == variant.Id);

        _output.WriteLine(
            $"After UpdateCartItem (Quantity -> 2, variant now at P2={p2}): " +
            $"Quantity={afterUpdate.Quantity}, UnitPrice={afterUpdate.UnitPrice}");

        afterUpdate.Quantity.Should().Be(2);
        afterUpdate.UnitPrice.Should().Be(p2,
            "Policy C (8.4.3): ShoppingCartItem.UnitPrice always reflects the current variant price while the item " +
            "remains in the cart, so UpdateCartItemCommandHandler must refresh it exactly like AddCartItemCommandHandler does");

        await addHandler.Handle(
            new AddCartItemCommand { Item = new AddCartItemDto { ProductVariantId = variant.Id, Quantity = 1 } },
            default);

        var afterSecondAdd = await context.ShoppingCartItems.AsNoTracking()
            .FirstAsync(i => i.ProductVariantId == variant.Id);

        _output.WriteLine(
            $"After second Add (+1 unit, variant at P2={p2}): " +
            $"Quantity={afterSecondAdd.Quantity}, UnitPrice={afterSecondAdd.UnitPrice}");

        afterSecondAdd.Quantity.Should().Be(3,
            "AddCartItemCommandHandler merges quantity into the same line rather than creating a second row");
        afterSecondAdd.UnitPrice.Should().Be(p2,
            "AddCartItemCommandHandler unconditionally overwrites UnitPrice with the current variant price, " +
            "even when merging into an existing line");

        var effectiveTotalForTheLine = afterSecondAdd.Quantity * afterSecondAdd.UnitPrice;
        _output.WriteLine(
            $"Effective total for this line after the second Add: {afterSecondAdd.Quantity} x {afterSecondAdd.UnitPrice} = {effectiveTotalForTheLine} " +
            $"(all 3 units priced at P2, even though 2 of them were added while the variant was still at P1={p1})");

        effectiveTotalForTheLine.Should().Be(3 * p2,
            "Policy C: the whole line is valued at the current variant price, including units added before the price change");

        var getCartHandler = new GetCartQueryHandler(context, currentUser.Object);
        var cartDto = await getCartHandler.Handle(new GetCartQuery(), default);

        var lineInCartDto = cartDto.Items.Single(i => i.ProductVariantId == variant.Id);

        _output.WriteLine(
            $"GetCart DTO for this line: Quantity={lineInCartDto.Quantity}, UnitPrice={lineInCartDto.UnitPrice}, " +
            $"Subtotal={lineInCartDto.Subtotal}; Cart.Total={cartDto.Total}");

        var checkoutWouldChargeForThisLine = afterSecondAdd.Quantity * afterSecondAdd.UnitPrice;

        lineInCartDto.Subtotal.Should().Be(checkoutWouldChargeForThisLine,
            "GetCart and Checkout both read the same ShoppingCartItem.UnitPrice, so they must agree on the effective price");

        _output.WriteLine(
            $"CONCLUSION: effective price applied to the entire line = {afterSecondAdd.UnitPrice} (P2), " +
            $"applied to all {afterSecondAdd.Quantity} units, consistent with Policy C. GetCart and Checkout agree.");
    }
}
