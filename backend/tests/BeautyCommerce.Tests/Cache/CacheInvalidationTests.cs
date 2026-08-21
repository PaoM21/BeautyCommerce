using BeautyCommerce.Application.Common.Behaviors;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Authentication.Commands.Register;
using BeautyCommerce.Application.Features.Authentication.DTOs;
using BeautyCommerce.Application.Features.Brands.Commands.CreateBrand;
using BeautyCommerce.Application.Features.Brands.DTOs;
using BeautyCommerce.Application.Features.Brands.Queries.GetAllBrands;
using BeautyCommerce.Application.Features.Categories.Commands.UpdateCategory;
using BeautyCommerce.Application.Features.Categories.DTOs;
using BeautyCommerce.Application.Features.Categories.Queries.GetAllCategories;
using BeautyCommerce.Application.Features.Reviews.Commands.CreateReview;
using BeautyCommerce.Application.Features.Reviews.DTOs;
using BeautyCommerce.Application.Features.Reviews.Queries.GetProductReviews;
using BeautyCommerce.Application.Features.Users.DTOs;
using BeautyCommerce.Application.Features.Users.Queries.GetUsers;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Domain.Enums;
using BeautyCommerce.Infrastructure.Persistence;
using BeautyCommerce.Infrastructure.Services;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BeautyCommerce.Tests.Cache;

public class CacheInvalidationTests
{
    private static CachingBehavior<TRequest, TResponse> NewBehavior<TRequest, TResponse>(ICacheService cache)
        where TRequest : notnull
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns((Guid?)null);

        return new CachingBehavior<TRequest, TResponse>(
            cache,
            currentUser.Object,
            NullLogger<CachingBehavior<TRequest, TResponse>>.Instance);
    }

    [Fact]
    public async Task Brands_Create_Invalidates_The_Cached_List()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        context.Brands.Add(new Brand { Name = "Seed Brand", Description = "x", IsActive = true });
        await context.SaveChangesAsync();

        var cache = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));
        var behavior = NewBehavior<GetAllBrandsQuery, List<BrandDto>>(cache);
        var queryHandler = new GetAllBrandsQueryHandler(context);

        Task<List<BrandDto>> RunQuery() =>
            behavior.Handle(new GetAllBrandsQuery(), _ => queryHandler.Handle(new GetAllBrandsQuery(), default), default);

        var first = await RunQuery();
        first.Should().HaveCount(1);

        context.Brands.Add(new Brand { Name = "Uncached Brand", Description = "x", IsActive = true });
        await context.SaveChangesAsync();

        var second = await RunQuery();
        second.Should().HaveCount(1);
        ReferenceEquals(first, second).Should().BeTrue("a cache hit returns the exact cached instance");

        var createHandler = new CreateBrandCommandHandler(context, cache);
        await createHandler.Handle(
            new CreateBrandCommand { Brand = new CreateBrandDto { Name = "New Brand", Description = "x" } },
            default);

        var third = await RunQuery();
        third.Should().HaveCount(3);
        ReferenceEquals(second, third).Should().BeFalse("invalidation must force a new instance, not reuse the stale one");
    }

    [Fact]
    public async Task Categories_Update_Invalidates_The_Cached_List()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var category = new Category { Name = "Original Name", Description = "x", IsActive = true };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var cache = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));
        var behavior = NewBehavior<GetAllCategoriesQuery, List<CategoryDto>>(cache);
        var queryHandler = new GetAllCategoriesQueryHandler(context);

        Task<List<CategoryDto>> RunQuery() =>
            behavior.Handle(new GetAllCategoriesQuery(), _ => queryHandler.Handle(new GetAllCategoriesQuery(), default), default);

        var first = await RunQuery();
        first.Should().ContainSingle(x => x.Name == "Original Name");

        var second = await RunQuery();
        ReferenceEquals(first, second).Should().BeTrue("a cache hit returns the exact cached instance");

        var updateHandler = new UpdateCategoryCommandHandler(context, cache);
        var updated = await updateHandler.Handle(
            new UpdateCategoryCommand
            {
                Id = category.Id,
                Category = new UpdateCategoryDto { Name = "Updated Name", Description = "x" }
            },
            default);
        updated.Should().BeTrue();

        var third = await RunQuery();
        third.Should().ContainSingle(x => x.Name == "Updated Name");
        ReferenceEquals(second, third).Should().BeFalse("invalidation must force a new instance, not reuse the stale one");
    }

    [Fact]
    public async Task Reviews_Create_Invalidates_The_Cached_List_For_That_Product()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var brand = new Brand { Name = "QA Brand", Description = "x" };
        var category = new Category { Name = "QA Category", Description = "x" };
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
            ProductId = product.Id,
            SKU = $"QA-SKU-{Guid.NewGuid():N}"[..20],
            Barcode = $"QA-BC-{Guid.NewGuid():N}"[..20],
            Price = 10m,
            Stock = 10
        };
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        var userId = Guid.NewGuid();

        var order = new Order
        {
            UserId = userId,
            OrderNumber = "ORD-CACHE-TEST",
            Status = OrderStatus.Delivered,
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

        var userService = new Mock<IUserService>();
        userService
            .Setup(x => x.GetUsersByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto>());

        var cache = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));
        var behavior = NewBehavior<GetProductReviewsQuery, List<ReviewDto>>(cache);
        var queryHandler = new GetProductReviewsQueryHandler(context, userService.Object);

        var query = new GetProductReviewsQuery { ProductId = product.Id };

        Task<List<ReviewDto>> RunQuery() =>
            behavior.Handle(query, _ => queryHandler.Handle(query, default), default);

        var first = await RunQuery();
        first.Should().BeEmpty();

        var second = await RunQuery();
        ReferenceEquals(first, second).Should().BeTrue("a cache hit returns the exact cached instance");

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);

        var createHandler = new CreateReviewCommandHandler(context, currentUser.Object, cache);
        await createHandler.Handle(
            new CreateReviewCommand
            {
                Review = new CreateReviewDto { ProductId = product.Id, Rating = 5, Comment = "Great!" }
            },
            default);

        var third = await RunQuery();
        third.Should().HaveCount(1, "the new review must show up immediately, not after up to 5 minutes");
        ReferenceEquals(second, third).Should().BeFalse("invalidation must force a new instance, not reuse the stale one");
    }

    [Fact]
    public async Task Register_Invalidates_The_Cached_User_List()
    {
        var identityService = new Mock<IIdentityService>();
        var newUserId = Guid.NewGuid();
        identityService
            .Setup(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(newUserId);

        var beforeRegister = new List<UserDto> { new() { Id = Guid.NewGuid(), FullName = "Existing User", Email = "e@test.local" } };
        var afterRegister = new List<UserDto>(beforeRegister)
        {
            new() { Id = newUserId, FullName = "New User", Email = "new@test.local" }
        };

        var userService = new Mock<IUserService>();
        userService.SetupSequence(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(beforeRegister)
            .ReturnsAsync(afterRegister);

        var cache = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));
        var behavior = NewBehavior<GetUsersQuery, List<UserDto>>(cache);
        var queryHandler = new GetUsersQueryHandler(userService.Object);

        Task<List<UserDto>> RunQuery() =>
            behavior.Handle(new GetUsersQuery(), _ => queryHandler.Handle(new GetUsersQuery(), default), default);

        var first = await RunQuery();
        first.Should().HaveCount(1);

        var second = await RunQuery();
        ReferenceEquals(first, second).Should().BeTrue("a cache hit returns the exact cached instance, without calling IUserService again");
        userService.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);

        var registerHandler = new RegisterCommandHandler(
            identityService.Object,
            cache,
            Mock.Of<IEmailService>(),
            NullLogger<RegisterCommandHandler>.Instance);
        await registerHandler.Handle(
            new RegisterCommand
            {
                User = new RegisterRequestDto { FirstName = "New", LastName = "User", Email = "new@test.local", Password = "Qa12345!" }
            },
            default);

        var third = await RunQuery();
        third.Should().HaveCount(2, "the admin must see the newly registered customer without waiting for the cache to expire");
        userService.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
