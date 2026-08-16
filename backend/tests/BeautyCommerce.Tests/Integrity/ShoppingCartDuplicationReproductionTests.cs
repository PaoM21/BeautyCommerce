using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.ShoppingCart.Commands.AddItem;
using BeautyCommerce.Application.Features.ShoppingCart.DTOs;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BeautyCommerce.Tests.Integrity;

public class ShoppingCartDuplicationReproductionTests
{
    [Fact]
    public async Task A_Second_ShoppingCart_For_The_Same_User_Is_Now_Rejected_By_The_Schema_Even_Bypassing_The_Handler()
    {
        var (contextA, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var brand = new Brand { Name = "QA Brand", Description = "QA" };
        var category = new Category { Name = "QA Category", Description = "QA" };
        contextA.Brands.Add(brand);
        contextA.Categories.Add(category);
        await contextA.SaveChangesAsync();

        var product = new Product
        {
            Name = "QA Product",
            Slug = $"qa-{Guid.NewGuid():N}",
            BrandId = brand.Id,
            CategoryId = category.Id
        };
        contextA.Products.Add(product);
        await contextA.SaveChangesAsync();

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            SKU = $"QA-SKU-{Guid.NewGuid():N}"[..20],
            Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
            Price = 10m,
            Stock = 10
        };
        contextA.ProductVariants.Add(variant);
        await contextA.SaveChangesAsync();

        var userId = Guid.NewGuid();

        // Request B: a normal, successful add-to-cart via the REAL,
        // unmodified handler for a brand-new user — creates ShoppingCart #1.
        await using var contextB = new BeautyCommerce.Infrastructure.Persistence.ApplicationDbContext(
            new DbContextOptionsBuilder<BeautyCommerce.Infrastructure.Persistence.ApplicationDbContext>()
                .UseSqlite(connection).Options,
            null);

        var currentUserB = new Mock<ICurrentUserService>();
        currentUserB.Setup(x => x.UserId).Returns(userId);

        var handlerB = new AddCartItemCommandHandler(contextB, currentUserB.Object);

        var cartIdFromB = await handlerB.Handle(
            new AddCartItemCommand { Item = new AddCartItemDto { ProductVariantId = variant.Id, Quantity = 1 } },
            default);

        cartIdFromB.Should().NotBeEmpty();

        // A raw, direct attempt to insert a SECOND ShoppingCart for the
        // same user — bypassing AddCartItemCommandHandler and its
        // savepoint-recovery logic entirely, going straight at the schema.
        contextA.ShoppingCarts.Add(new ShoppingCart { UserId = userId });
        var rawInsertAct = async () => await contextA.SaveChangesAsync();

        // 8.3.4 regression: the schema itself now rejects this, regardless
        // of which code path attempts it — the UNIQUE(UserId) index is the
        // real guarantee, not something that depends on going through the
        // handler correctly.
        await rawInsertAct.Should().ThrowAsync<DbUpdateException>(
            "ShoppingCarts.UserId now has a UNIQUE index — a second row for the same user must be rejected at the schema level, " +
            "even when the attempt bypasses AddCartItemCommandHandler completely");

        var cartsForUser = await contextA.ShoppingCarts.AsNoTracking()
            .Where(c => c.UserId == userId)
            .ToListAsync();

        cartsForUser.Should().ContainSingle("the rejected raw insert must not have left a second row behind");
    }
}
