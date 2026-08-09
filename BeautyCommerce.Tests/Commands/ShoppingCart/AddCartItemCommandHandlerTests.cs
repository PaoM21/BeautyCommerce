using BeautyCommerce.Application.Features.ShoppingCart.Commands.AddItem;
using BeautyCommerce.Application.Features.ShoppingCart.DTOs;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace BeautyCommerce.Tests.Commands.ShoppingCart;

public class AddCartItemCommandHandlerTests
{
    [Fact]
    public async Task Should_Add_Item_To_New_Cart()
    {
        var context = DbContextHelper.CreateDbContext();

        var variant = new BeautyCommerce.Domain.Entities.ProductVariant
        {
            Id = Guid.NewGuid(),
            Price = 15m,
            Stock = 20
        };

        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService();

        var handler = new AddCartItemCommandHandler(context, currentUser);

        var command = new AddCartItemCommand
        {
            Item = new AddCartItemDto
            {
                ProductVariantId = variant.Id,
                Quantity = 2
            }
        };

        var cartId = await handler.Handle(command, default);

        cartId.Should().NotBeEmpty();
        var cart = context.ShoppingCarts.FirstOrDefault(s => s.Id == cartId);
        cart.Should().NotBeNull();
        cart!.Items.Count.Should().Be(1);
    }
}
