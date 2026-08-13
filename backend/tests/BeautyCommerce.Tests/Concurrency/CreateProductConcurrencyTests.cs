using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Products.Commands.CreateProduct;
using BeautyCommerce.Application.Features.Products.DTOs;
using BeautyCommerce.Infrastructure.Persistence;
using BeautyCommerce.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BeautyCommerce.Tests.Concurrency;

// 6.4.5: IProductVariantIdentifierGenerator checks PostgreSQL for an
// existing SKU/Barcode before returning a candidate (see
// ProductVariantIdentifierGenerator.GenerateUniqueAsync), but that
// check-then-use pattern is exactly the shape that can race under real
// concurrency. This proves two products created at the same instant, each
// through the real CreateProductCommandHandler against real PostgreSQL,
// still end up with four distinct identifiers (2 SKUs + 2 Barcodes) — not
// just "usually", every round.
[Trait("Category", "Integration")]
public class CreateProductConcurrencyTests : IAsyncLifetime
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=BeautyCommerceDb;Username=postgres;";

    private Guid _brandId;
    private Guid _categoryId;
    private readonly List<Guid> _createdProductIds = [];

    private static ApplicationDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(ConnectionString)
                .Options,
            null);

    public async Task InitializeAsync()
    {
        await using var context = NewContext();
        _brandId = await context.Brands.Select(x => x.Id).FirstAsync();
        _categoryId = await context.Categories.Select(x => x.Id).FirstAsync();
    }

    public async Task DisposeAsync()
    {
        if (_createdProductIds.Count == 0)
            return;

        await using var context = NewContext();

        var variants = await context.ProductVariants
            .Where(v => _createdProductIds.Contains(v.ProductId))
            .ToListAsync();
        context.ProductVariants.RemoveRange(variants);
        await context.SaveChangesAsync();

        var products = await context.Products
            .Where(p => _createdProductIds.Contains(p.Id))
            .ToListAsync();
        context.Products.RemoveRange(products);
        await context.SaveChangesAsync();
    }

    private async Task<(Guid ProductId, string Sku, string Barcode)> CreateProductAsync(ApplicationDbContext context, string name)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        var cache = new Mock<ICacheService>();
        var identifierGenerator = new ProductVariantIdentifierGenerator(context);
        var handler = new CreateProductCommandHandler(context, cache.Object, identifierGenerator);

        var productId = await handler.Handle(new CreateProductCommand
        {
            Name = name,
            Description = "Created by CreateProductConcurrencyTests; removed in DisposeAsync.",
            BrandId = _brandId,
            CategoryId = _categoryId,
            Variants = [new CreateVariantDto { Price = 10m, Stock = 1 }]
        }, default);

        await transaction.CommitAsync();

        var variant = await context.ProductVariants
            .AsNoTracking()
            .FirstAsync(v => v.ProductId == productId);

        return (productId, variant.SKU, variant.Barcode);
    }

    [Fact]
    public async Task Concurrent_Product_Creations_Never_Produce_Colliding_Identifiers()
    {
        const int rounds = 10;

        for (var round = 1; round <= rounds; round++)
        {
            await using var contextA = NewContext();
            await using var contextB = NewContext();

            var taskA = CreateProductAsync(contextA, $"QA Concurrency Product A r{round} {Guid.NewGuid():N}");
            var taskB = CreateProductAsync(contextB, $"QA Concurrency Product B r{round} {Guid.NewGuid():N}");

            var results = await Task.WhenAll(taskA, taskB);

            _createdProductIds.Add(results[0].ProductId);
            _createdProductIds.Add(results[1].ProductId);

            results[0].Sku.Should().NotBe(results[1].Sku, $"round {round}: two concurrently created variants must not share a SKU");
            results[0].Barcode.Should().NotBe(results[1].Barcode, $"round {round}: two concurrently created variants must not share a Barcode");
        }
    }
}
