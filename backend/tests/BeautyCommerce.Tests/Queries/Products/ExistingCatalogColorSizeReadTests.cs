using BeautyCommerce.Application.Features.Products.Queries.GetProductById;
using BeautyCommerce.Infrastructure.Persistence;
using BeautyCommerce.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Tests.Queries.Products;

[Trait("Category", "Integration")]
public class ExistingCatalogColorSizeReadTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=BeautyCommerceDb;Username=postgres;";

    private static ApplicationDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(ConnectionString)
                .Options,
            null);

    [Fact]
    public async Task GetProductByIdQuery_Returns_Real_Color_And_Size_For_An_Existing_Catalog_Product()
    {
        await using var context = NewContext();

        var target = await context.ProductVariants
            .AsNoTracking()
            .Where(v => v.Color != "" && v.Size != "")
            .Select(v => new { v.ProductId, v.Id, v.Color, v.Size })
            .FirstOrDefaultAsync();

        target.Should().NotBeNull(
            "the 6.5-B.2 audit found 79 real variants with non-blank Color/Size — this must still be true unless the catalog changed");

        var repository = new ProductRepository(context);
        var handler = new GetProductByIdHandler(repository);

        var product = await handler.Handle(new GetProductByIdQuery { Id = target!.ProductId }, default);

        var variant = product.Variants.Should().ContainSingle(v => v.Id == target.Id).Subject;

        variant.Color.Should().Be(target.Color, "the API must now return the real, already-existing Color value");
        variant.Size.Should().Be(target.Size, "the API must now return the real, already-existing Size value");
    }
}
