using BeautyCommerce.API.Middlewares;
using BeautyCommerce.Application.Common.Behaviors;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.ShoppingCart.Commands.AddItem;
using BeautyCommerce.Application.Features.ShoppingCart.DTOs;
using BeautyCommerce.Application.Features.ShoppingCart.Validators;
using BeautyCommerce.Application.Features.Wishlist.Commands.AddWishlist;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Persistence;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BeautyCommerce.Tests.Concurrency;

[Trait("Category", "Integration")]
public class DbConstraintExceptionReproductionTests : IAsyncLifetime
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=BeautyCommerceDb;Username=postgres;";

    private Guid _productId;
    private Guid _variantId;
    private readonly List<Guid> _usedUserIds = [];

    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(ConnectionString).Options, null);

    public async Task InitializeAsync()
    {
        await using var context = NewContext();

        var brandId = await context.Brands.Select(x => x.Id).FirstAsync();
        var categoryId = await context.Categories.Select(x => x.Id).FirstAsync();

        var product = new Product
        {
            Name = "QA DbConstraint Product",
            Slug = $"qa-dbconstraint-{Guid.NewGuid():N}",
            ShortDescription = "QA test product, safe to delete.",
            Description = "Created by DbConstraintExceptionReproductionTests; removed in DisposeAsync.",
            BrandId = brandId,
            CategoryId = categoryId
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var variant = new ProductVariant
        {
            ProductId = product.Id,
            SKU = $"QA-DBC-{Guid.NewGuid():N}"[..20],
            Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
            Price = 10m,
            Stock = 1000,
            MinimumStock = 0
        };
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        _productId = product.Id;
        _variantId = variant.Id;
    }

    public async Task DisposeAsync()
    {
        await using var context = NewContext();

        if (_usedUserIds.Count > 0)
        {
            var cartIds = await context.ShoppingCarts.Where(c => _usedUserIds.Contains(c.UserId)).Select(c => c.Id).ToListAsync();
            await context.ShoppingCartItems.Where(i => cartIds.Contains(i.ShoppingCartId)).ExecuteDeleteAsync();
            await context.ShoppingCarts.Where(c => cartIds.Contains(c.Id)).ExecuteDeleteAsync();
            await context.WishlistItems.IgnoreQueryFilters().Where(i => _usedUserIds.Contains(i.UserId)).ExecuteDeleteAsync();
        }

        var variant = await context.ProductVariants.FindAsync(_variantId);
        if (variant != null) context.ProductVariants.Remove(variant);

        var product = await context.Products.FindAsync(_productId);
        if (product != null) context.Products.Remove(product);

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task AddCartItem_With_Zero_Quantity_Is_Rejected_By_Validation_Before_Reaching_The_Database()
    {
        var userId = Guid.NewGuid();
        _usedUserIds.Add(userId);

        await using var context = NewContext();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);

        var handler = new AddCartItemCommandHandler(context, currentUser.Object);
        var validation = new ValidationBehavior<AddCartItemCommand, Guid>([new AddCartItemValidator()]);

        var command = new AddCartItemCommand
        {
            Item = new AddCartItemDto { ProductVariantId = _variantId, Quantity = 0 }
        };

        var act = async () => await validation.Handle(command, _ => handler.Handle(command, default), default);

        var thrown = await act.Should().ThrowAsync<ValidationException>();
        var (status, _) = GlobalExceptionHandler.Map(thrown.Which);
        status.Should().Be(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);

        var items = await context.ShoppingCartItems.AsNoTracking()
            .Where(i => i.ProductVariantId == _variantId).ToListAsync();
        items.Should().BeEmpty("validation must reject the command before any ShoppingCartItem is created");
    }

    [Fact]
    public async Task AddCartItem_Bypassing_The_Validator_Still_Gets_Rejected_By_The_Real_Check_Constraint()
    {
        var userId = Guid.NewGuid();
        _usedUserIds.Add(userId);

        await using var context = NewContext();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);

        var handler = new AddCartItemCommandHandler(context, currentUser.Object);

        var command = new AddCartItemCommand
        {
            Item = new AddCartItemDto { ProductVariantId = _variantId, Quantity = 0 }
        };

        var act = async () => await handler.Handle(command, default);

        await act.Should().ThrowAsync<DbUpdateException>(
            "CK_ShoppingCartItems_Quantity_Positive must remain the last line of defense");
    }

    private async Task RunAddWishlistAsync(ApplicationDbContext context, Guid userId)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);

        var handler = new AddWishlistCommandHandler(context, currentUser.Object);
        var behavior = new TransactionBehavior<AddWishlistCommand, MediatR.Unit>(
            context, NullLogger<TransactionBehavior<AddWishlistCommand, MediatR.Unit>>.Instance);

        var command = new AddWishlistCommand { ProductId = _productId };

        await behavior.Handle(
            command,
            async _ => { await handler.Handle(command, default); return MediatR.Unit.Value; },
            default);
    }

    [Fact]
    public async Task Two_Concurrent_AddWishlist_For_The_Same_Product_Both_Succeed_And_Only_One_Item_Is_Created()
    {
        const int rounds = 10;

        for (var round = 1; round <= rounds; round++)
        {
            var userId = Guid.NewGuid();
            _usedUserIds.Add(userId);

            await using var contextA = NewContext();
            await using var contextB = NewContext();

            var taskA = RunAddWishlistAsync(contextA, userId);
            var taskB = RunAddWishlistAsync(contextB, userId);

            // Neither task is awaited individually first — both real,
            // independent Npgsql connections race genuinely via Task.WhenAll.
            var act = async () => await Task.WhenAll(taskA, taskB);

            await act.Should().NotThrowAsync(
                $"round {round}: the loser of the race must resolve idempotently instead of surfacing the unique-constraint violation");

            await using var verify = NewContext();
            var items = await verify.WishlistItems.AsNoTracking()
                .Where(x => x.UserId == userId && x.ProductId == _productId).ToListAsync();

            items.Should().ContainSingle(
                $"round {round}: both requests targeted the same (UserId, ProductId) pair — only one row must exist");
        }
    }
}
