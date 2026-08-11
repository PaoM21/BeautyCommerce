using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.ProductVariants.Commands.UpdateStock;
using BeautyCommerce.Application.Features.ProductVariants.DTOs;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BeautyCommerce.Tests.Commands.ProductVariants;

public class UpdateStockCommandHandlerTests
{
    [Fact]
    public async Task Should_Adjust_Variant_Stock_By_Quantity()
    {
        var context = DbContextHelper.CreateDbContext();

        var variant = new BeautyCommerce.Domain.Entities.ProductVariant
        {
            Id = Guid.NewGuid(),
            Price = 12.5m,
            Stock = 5
        };

        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync(default);

        var cache = new Mock<ICacheService>();

        var handler = new UpdateStockCommandHandler(
            context,
            cache.Object,
            NullLogger<UpdateStockCommandHandler>.Instance);

        var command = new UpdateStockCommand
        {
            Stock = new UpdateStockDto
            {
                ProductVariantId = variant.Id,
                Quantity = -3
            }
        };

        await handler.Handle(command, default);

        var updated = await context.ProductVariants.FindAsync(variant.Id);
        updated!.Stock.Should().Be(2);
    }
}
