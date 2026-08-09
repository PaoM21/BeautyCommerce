using BeautyCommerce.Application.Features.Brands.Commands.CreateBrand;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Application.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace BeautyCommerce.Application.Tests.Features.Brands;

public class CreateBrandCommandHandlerTests
{
    [Fact]
    public async Task Should_Create_Brand()
    {
        // Arrange
        var context = ApplicationDbContextFactory.Create();

        var handler = new CreateBrandCommandHandler(context);

        var command = new CreateBrandCommand
        {
            Name = "Maybelline",
            Description = "Marca internacional",
            LogoUrl = "logo.png"
        };

        // Act
        var id = await handler.Handle(command, CancellationToken.None);

        // Assert
        context.Brands.Count()
            .Should()
            .Be(1);

        context.Brands.First().Name
            .Should()
            .Be("Maybelline");
    }
}
