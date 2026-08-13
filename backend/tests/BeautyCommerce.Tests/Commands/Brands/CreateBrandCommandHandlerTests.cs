using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Brands.Commands.CreateBrand;
using BeautyCommerce.Application.Features.Brands.DTOs;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace BeautyCommerce.Tests.Commands.Brands;

public class CreateBrandCommandHandlerTests
{
    [Fact]
    public async Task Should_Create_Brand()
    {
        // Arrange
        var context = DbContextHelper.CreateDbContext();
        var cache = new Mock<ICacheService>();

        var handler = new CreateBrandCommandHandler(context, cache.Object);

        var command = new CreateBrandCommand
        {
            Brand = new CreateBrandDto
            {
                Name = "Maybelline",
                Description = "Maquillaje",
                LogoUrl = "logo.png"
            }
        };

        // Act
        var id = await handler.Handle(command, default);

        // Assert
        id.Should().NotBeEmpty();

        context.Brands.Count().Should().Be(1);

        cache.Verify(x => x.InvalidateTagAsync("Brands"), Times.Once);
    }
}