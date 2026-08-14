using BeautyCommerce.API.Middlewares;
using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.ShoppingCart.Commands.AddItem;
using BeautyCommerce.Application.Features.ShoppingCart.Commands.RemoveItem;
using BeautyCommerce.Application.Features.ShoppingCart.Commands.UpdateItem;
using BeautyCommerce.Application.Features.ShoppingCart.DTOs;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Services;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace BeautyCommerce.Tests.Integrity;

public class ExceptionContractReproductionTests
{
    private static async Task<(BeautyCommerce.Infrastructure.Persistence.ApplicationDbContext Context, ProductVariant Variant, Microsoft.Data.Sqlite.SqliteConnection Connection)> SeedAsync()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();

        var brand = new Brand { Name = "QA Brand", Description = "QA" };
        var category = new Category { Name = "QA Category", Description = "QA" };
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "QA Product",
            Slug = $"qa-{Guid.NewGuid():N}",
            BrandId = brand.Id,
            CategoryId = category.Id
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            SKU = $"QA-SKU-{Guid.NewGuid():N}"[..20],
            Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
            Price = 10m,
            Stock = 1
        };
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        return (context, variant, connection);
    }

    [Fact]
    public async Task Unauthenticated_Add_Vs_Remove_Vs_Update_Exception_Contract()
    {
        var (context, variant, connection) = await SeedAsync();
        using var _ = connection;

        var noUser = new Mock<ICurrentUserService>();
        noUser.Setup(x => x.UserId).Returns((Guid?)null);

        var addHandler = new AddCartItemCommandHandler(context, noUser.Object);
        var addAct = async () => await addHandler.Handle(
            new AddCartItemCommand { Item = new AddCartItemDto { ProductVariantId = variant.Id, Quantity = 1 } },
            default);
        var addEx = await addAct.Should().ThrowAsync<UnauthorizedAccessException>();

        var removeHandler = new RemoveCartItemCommandHandler(context, noUser.Object);
        var removeAct = async () => await removeHandler.Handle(
            new RemoveCartItemCommand { ProductVariantId = variant.Id },
            default);
        var removeEx = await removeAct.Should().ThrowAsync<UnauthorizedAccessException>();

        var updateHandler = new UpdateCartItemCommandHandler(context, noUser.Object);
        var updateAct = async () => await updateHandler.Handle(
            new UpdateCartItemCommand { Item = new UpdateCartItemDto { ProductVariantId = variant.Id, Quantity = 1 } },
            default);
        var updateEx = await updateAct.Should().ThrowAsync<UnauthorizedAccessException>();

        addEx.Which.Message.Should().Be(removeEx.Which.Message);
        addEx.Which.Message.Should().Be(updateEx.Which.Message);

        var (addStatus, addTitle) = GlobalExceptionHandler.Map(addEx.Which);
        var (removeStatus, removeTitle) = GlobalExceptionHandler.Map(removeEx.Which);
        addStatus.Should().Be(StatusCodes.Status401Unauthorized);
        removeStatus.Should().Be(StatusCodes.Status401Unauthorized);
        addTitle.Should().Be(removeTitle, "both map through the same UnauthorizedAccessException branch, so the Title is identical");
    }

    [Fact]
    public async Task Nonexistent_Target_Add_Vs_Remove_Vs_Update_Exception_Contract()
    {
        var (context, variant, connection) = await SeedAsync();
        using var _ = connection;
        var userId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);

        var missingVariantId = Guid.NewGuid();
        var addHandler = new AddCartItemCommandHandler(context, currentUser.Object);
        var addAct = async () => await addHandler.Handle(
            new AddCartItemCommand { Item = new AddCartItemDto { ProductVariantId = missingVariantId, Quantity = 1 } },
            default);
        var addEx = await addAct.Should().ThrowAsync<NotFoundException>();

        var removeHandler = new RemoveCartItemCommandHandler(context, currentUser.Object);
        var removeAct = async () => await removeHandler.Handle(
            new RemoveCartItemCommand { ProductVariantId = variant.Id },
            default);
        var removeEx = await removeAct.Should().ThrowAsync<NotFoundException>();

        var updateHandler = new UpdateCartItemCommandHandler(context, currentUser.Object);
        var updateAct = async () => await updateHandler.Handle(
            new UpdateCartItemCommand { Item = new UpdateCartItemDto { ProductVariantId = variant.Id, Quantity = 1 } },
            default);
        var updateEx = await updateAct.Should().ThrowAsync<NotFoundException>();

        addEx.Which.Message.Should().Be("La variante del producto no existe.");
        removeEx.Which.Message.Should().Be("Carrito no encontrado.");
        updateEx.Which.Message.Should().Be("Carrito no encontrado.");

        var (addStatus, addTitle) = GlobalExceptionHandler.Map(addEx.Which);
        var (removeStatus, removeTitle) = GlobalExceptionHandler.Map(removeEx.Which);

        addStatus.Should().Be(StatusCodes.Status404NotFound);
        removeStatus.Should().Be(StatusCodes.Status404NotFound);
        addTitle.Should().Be(removeTitle, "Title is identical (both 'Recurso no encontrado.') — only Detail's language differs");
    }

    [Fact]
    public async Task Insufficient_Stock_Add_Vs_Update_Exception_Contract()
    {
        var (context, variant, connection) = await SeedAsync(); // Stock = 1
        using var _ = connection;
        var userId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);

        var addHandler = new AddCartItemCommandHandler(context, currentUser.Object);
        var addAct = async () => await addHandler.Handle(
            new AddCartItemCommand { Item = new AddCartItemDto { ProductVariantId = variant.Id, Quantity = 5 } },
            default);
        var addEx = await addAct.Should().ThrowAsync<BadRequestException>();

        var cart = new ShoppingCart { UserId = userId };
        context.ShoppingCarts.Add(cart);
        await context.SaveChangesAsync();
        context.ShoppingCartItems.Add(new ShoppingCartItem
        {
            ShoppingCartId = cart.Id,
            ProductVariantId = variant.Id,
            Quantity = 1,
            UnitPrice = variant.Price
        });
        await context.SaveChangesAsync();

        var updateHandler = new UpdateCartItemCommandHandler(context, currentUser.Object);
        var updateAct = async () => await updateHandler.Handle(
            new UpdateCartItemCommand { Item = new UpdateCartItemDto { ProductVariantId = variant.Id, Quantity = 5 } },
            default);
        var updateEx = await updateAct.Should().ThrowAsync<BadRequestException>();

        addEx.Which.Message.Should().Be("No hay stock suficiente.");
        updateEx.Which.Message.Should().Be("No hay stock suficiente.");

        var (addStatus, addTitle) = GlobalExceptionHandler.Map(addEx.Which);
        var (updateStatus, updateTitle) = GlobalExceptionHandler.Map(updateEx.Which);

        addStatus.Should().Be(StatusCodes.Status400BadRequest);
        updateStatus.Should().Be(StatusCodes.Status400BadRequest);
        addTitle.Should().Be(updateTitle,
            "both now throw BadRequestException for the identical real-world condition, so Title ('Solicitud inválida.') must match too — " +
            "this is the fix: Add previously had a different Title via InvalidOperationException");
    }

    [Fact]
    public async Task InventoryService_Zero_Quantity_Leaks_The_CSharp_Parameter_Name_Into_The_Client_Facing_Message()
    {
        var context = Mock.Of<IApplicationDbContext>(); // never touched: the quantity<=0 check is the first line
        var cache = new Mock<ICacheService>();
        var inventoryService = new InventoryService(context, cache.Object);

        var act = async () => await inventoryService.RegisterEntryAsync(
            Guid.NewGuid(), 0, "QA reason", null, default);

        var ex = await act.Should().ThrowAsync<ArgumentException>();

        ex.Which.Message.Should().Be("La cantidad debe ser mayor que cero.",
            "the ArgumentException constructor no longer passes a paramName, so the C# parameter name must not leak into the message");

        var (status, title) = GlobalExceptionHandler.Map(ex.Which);
        status.Should().Be(StatusCodes.Status400BadRequest);
        title.Should().Be("Solicitud inválida.");
    }
}
