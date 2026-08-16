using BeautyCommerce.Application.Common.Behaviors;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Application.Features.Products.DTOs;
using BeautyCommerce.Application.Features.Products.Queries.GetProducts;
using BeautyCommerce.Application.Features.Reviews.Commands.CreateReview;
using BeautyCommerce.Application.Features.Reviews.DTOs;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Domain.Enums;
using BeautyCommerce.Infrastructure.Persistence;
using BeautyCommerce.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BeautyCommerce.Tests.Cache;

[Trait("Category", "Integration")]
public class ProductsRatingCacheInvalidationTests : IAsyncLifetime
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=BeautyCommerceDb;Username=postgres;";

    private Guid _brandId;
    private Guid _categoryId;
    private Guid _productId;
    private Guid _variantId;
    private Guid _orderId;
    private Guid _reviewerId;

    private static ApplicationDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(ConnectionString)
                .Options,
            null);

    public async Task InitializeAsync()
    {
        await using var context = NewContext();

        var brand = new Brand { Name = "QA RatingCache Brand", Description = "QA test brand, safe to delete." };
        var category = new Category { Name = "QA RatingCache Category", Description = "QA test category, safe to delete." };
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "QA RatingCache Product",
            Slug = $"qa-ratingcache-{Guid.NewGuid():N}",
            ShortDescription = "QA test product, safe to delete.",
            Description = "Created by ProductsRatingCacheInvalidationTests.",
            BrandId = brand.Id,
            CategoryId = category.Id
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var variant = new ProductVariant
        {
            ProductId = product.Id,
            SKU = $"QA-SKU-{Guid.NewGuid():N}"[..20],
            Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
            Price = 10m,
            Stock = 10
        };
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        var reviewerId = Guid.NewGuid();

        var order = new Order
        {
            UserId = reviewerId,
            OrderNumber = $"ORD-QA-RATINGCACHE-{Guid.NewGuid():N}"[..30],
            Status = OrderStatus.Delivered,
            SubTotal = 10m,
            Total = 10m,
            OrderDate = DateTime.UtcNow
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        context.OrderItems.Add(new OrderItem
        {
            OrderId = order.Id,
            ProductVariantId = variant.Id,
            Quantity = 1,
            UnitPrice = 10m
        });
        await context.SaveChangesAsync();

        _brandId = brand.Id;
        _categoryId = category.Id;
        _productId = product.Id;
        _variantId = variant.Id;
        _orderId = order.Id;
        _reviewerId = reviewerId;
    }

    public async Task DisposeAsync()
    {
        await using var context = NewContext();

        var reviews = await context.Reviews.IgnoreQueryFilters()
            .Where(x => x.ProductId == _productId).ToListAsync();
        context.Reviews.RemoveRange(reviews);
        await context.SaveChangesAsync();

        var orderItems = await context.OrderItems.IgnoreQueryFilters()
            .Where(x => x.OrderId == _orderId).ToListAsync();
        context.OrderItems.RemoveRange(orderItems);
        await context.SaveChangesAsync();

        var order = await context.Orders.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == _orderId);
        if (order != null) context.Orders.Remove(order);
        await context.SaveChangesAsync();

        var variant = await context.ProductVariants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == _variantId);
        if (variant != null) context.ProductVariants.Remove(variant);
        await context.SaveChangesAsync();

        var product = await context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == _productId);
        if (product != null) context.Products.Remove(product);
        await context.SaveChangesAsync();

        var category = await context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == _categoryId);
        if (category != null) context.Categories.Remove(category);

        var brand = await context.Brands.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == _brandId);
        if (brand != null) context.Brands.Remove(brand);

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Creating_A_Review_Invalidates_The_Cached_Products_Listing()
    {
        await using var context = NewContext();

        var cache = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns((Guid?)null);

        var behavior = new CachingBehavior<GetProductsQuery, PagedResult<ProductListItemDto>>(
            cache, currentUser.Object, NullLogger<CachingBehavior<GetProductsQuery, PagedResult<ProductListItemDto>>>.Instance);

        var queryHandler = new GetProductsQueryHandler(context);
        var query = new GetProductsQuery { Filter = new ProductFilterDto { Search = "QA RatingCache Product" } };

        Task<PagedResult<ProductListItemDto>> RunQuery() =>
            behavior.Handle(query, _ => queryHandler.Handle(query, default), default);

        var first = await RunQuery();
        var firstItem = first.Items.Should().ContainSingle().Subject;
        firstItem.AverageRating.Should().BeNull();
        firstItem.ReviewCount.Should().Be(0);

        var second = await RunQuery();
        ReferenceEquals(first, second).Should().BeTrue("a cache hit returns the exact cached instance");

        var reviewerUser = new Mock<ICurrentUserService>();
        reviewerUser.Setup(x => x.UserId).Returns(_reviewerId);

        var createReviewHandler = new CreateReviewCommandHandler(context, reviewerUser.Object, cache);
        await createReviewHandler.Handle(
            new CreateReviewCommand
            {
                Review = new CreateReviewDto { ProductId = _productId, Rating = 5, Comment = "QA cache test review" }
            },
            default);

        var third = await RunQuery();
        ReferenceEquals(second, third).Should().BeFalse("invalidation must force a new instance, not reuse the stale one");

        var thirdItem = third.Items.Should().ContainSingle().Subject;
        thirdItem.AverageRating.Should().Be(5m, "the listing must reflect the new review immediately, not after up to 5 minutes");
        thirdItem.ReviewCount.Should().Be(1);
    }
}
