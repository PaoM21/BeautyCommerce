using BeautyCommerce.Application.Features.Products.Commands.UpdateStock;
using BeautyCommerce.Application.Features.Products.DTOs;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;

namespace BeautyCommerce.Tests.Commands.Products;

public class UpdateStockCommandHandlerTests
{
    [Fact]
    public async Task Should_Update_Stock_For_Variant()
    {
        var context = DbContextHelper.CreateDbContext();

        // Seed variant
        var variant = new BeautyCommerce.Domain.Entities.ProductVariant
        {
            Id = Guid.NewGuid(),
            Price = 5.0m,
            Stock = 2
        };

        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync(default);

        var handler = new UpdateStockCommandHandler(context, new Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateStockCommandHandler>());

        var command = new UpdateStockCommand
        {
            Stock = new UpdateStockDto
            {
                ProductVariantId = variant.Id,
                Stock = 10
            }
        };

        await handler.Handle(command, default);

        var updated = await context.ProductVariants.FindAsync(variant.Id);
        updated!.Stock.Should().Be(10);
    }
}
