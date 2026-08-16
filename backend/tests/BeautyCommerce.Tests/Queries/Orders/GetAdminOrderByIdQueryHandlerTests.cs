using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Orders.Queries.GetAdminOrderById;
using BeautyCommerce.Application.Features.Users.DTOs;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Domain.Enums;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace BeautyCommerce.Tests.Queries.Orders;

public class GetAdminOrderByIdQueryHandlerTests
{
    [Fact]
    public async Task Returns_The_Real_SubTotal_ShippingCost_Tax_And_Total()
    {
        var context = DbContextHelper.CreateDbContext();

        var brand = new Brand { Name = "Order Test Brand" };
        var category = new Category { Name = "Order Test Category" };
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Order Test Product",
            Slug = "order-test-product",
            BrandId = brand.Id,
            CategoryId = category.Id
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var variant = new ProductVariant
        {
            ProductId = product.Id,
            SKU = "SKU-ORDERTEST1",
            Barcode = "BC-ORDERTEST1",
            Color = "purple",
            Size = "M",
            Price = 20m,
            Stock = 10
        };
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var order = new Order
        {
            UserId = userId,
            OrderNumber = "ORD-TEST-001",
            Status = OrderStatus.Paid,
            SubTotal = 40m,
            ShippingCost = 5m,
            Tax = 3m,
            Total = 48m,
            OrderDate = DateTime.UtcNow,
            TransactionId = "tx-test-001"
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        context.OrderItems.Add(new OrderItem
        {
            OrderId = order.Id,
            ProductVariantId = variant.Id,
            Quantity = 2,
            UnitPrice = 20m
        });
        await context.SaveChangesAsync();

        var userService = new Mock<IUserService>();
        userService
            .Setup(x => x.GetUsersByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto>
            {
                [userId] = new UserDto { Id = userId, FullName = "Test Customer", Email = "customer@test.local" }
            });

        var handler = new GetAdminOrderByIdQueryHandler(context, userService.Object);

        var result = await handler.Handle(new GetAdminOrderByIdQuery { Id = order.Id }, default);

        result.Should().NotBeNull();
        result!.SubTotal.Should().Be(40m, "the real stored SubTotal must reach the DTO, not the decimal default of 0");
        result.ShippingCost.Should().Be(5m, "the real stored ShippingCost must reach the DTO, not the decimal default of 0");
        result.Tax.Should().Be(3m, "the real stored Tax must reach the DTO, not the decimal default of 0");
        result.Total.Should().Be(48m, "Total must remain correct, unaffected by adding the other three fields");

        result.OrderNumber.Should().Be("ORD-TEST-001");
        result.Status.Should().Be(OrderStatus.Paid.ToString());
        result.TransactionId.Should().Be("tx-test-001");
        result.CustomerName.Should().Be("Test Customer");
        result.CustomerEmail.Should().Be("customer@test.local");

        result.Items.Should().ContainSingle();
        var item = result.Items[0];
        item.ProductVariantId.Should().Be(variant.Id);
        item.ProductName.Should().Be("Order Test Product");
        item.Color.Should().Be("purple");
        item.Size.Should().Be("M");
        item.Quantity.Should().Be(2);
        item.UnitPrice.Should().Be(20m);
        item.Subtotal.Should().Be(40m);
    }
}
