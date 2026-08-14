using BeautyCommerce.Application.Common.Behaviors;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Categories.Commands.CreateCategory;
using BeautyCommerce.Application.Features.Categories.Commands.UpdateCategory;
using BeautyCommerce.Application.Features.Categories.DTOs;
using BeautyCommerce.Application.Features.Categories.Validators;
using BeautyCommerce.Application.Features.Products.Commands.CreateProduct;
using BeautyCommerce.Application.Features.Products.DTOs;
using BeautyCommerce.Application.Features.Products.Validators;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BeautyCommerce.Tests.Integrity;

// 7.8.2 reproduced (and this file originally proved) that ImageUrl accepted
// and persisted any string verbatim, for both Product and Category, in
// create and update. 7.8.3 designed, and 7.8.4 implements, the minimal fix:
// a FluentValidation rule (CreateProductValidator, plus new
// CreateCategoryValidator/UpdateCategoryValidator) requiring an absolute
// http/https URL — the only shape 100% of real ImageUrl data in the
// database actually uses. No domain restriction, no HTTPS-only requirement,
// no relative URLs: none of those are supported by real precedent, so none
// were added. These tests prove the regression is closed through the REAL
// ValidationBehavior + real validator + real handler pipeline.
public class ImageUrlValidationReproductionTests
{
    private static async Task<Guid> TryCreateProductWithImageAsync(
        BeautyCommerce.Infrastructure.Persistence.ApplicationDbContext context,
        Guid brandId,
        Guid categoryId,
        string imageUrl)
    {
        var command = new CreateProductCommand
        {
            Name = "QA Product",
            BrandId = brandId,
            CategoryId = categoryId,
            Variants = [new CreateVariantDto { Price = 10m, Stock = 5, Color = "purple", Size = "M" }],
            Images = [imageUrl]
        };

        var cache = new Mock<ICacheService>();
        var identifierGenerator = new Mock<IProductVariantIdentifierGenerator>();
        identifierGenerator.Setup(x => x.GenerateSkuAsync(It.IsAny<CancellationToken>())).ReturnsAsync($"QA-SKU-{Guid.NewGuid():N}"[..20]);
        identifierGenerator.Setup(x => x.GenerateBarcodeAsync(It.IsAny<CancellationToken>())).ReturnsAsync($"QA-BC-{Guid.NewGuid():N}"[..20]);

        var handler = new CreateProductCommandHandler(context, cache.Object, identifierGenerator.Object);

        // The REAL validation pipeline: ValidationBehavior resolves every
        // registered IValidator<CreateProductCommand> — here, the real,
        // now-updated CreateProductValidator.
        var validationBehavior = new ValidationBehavior<CreateProductCommand, Guid>(
            new IValidator<CreateProductCommand>[] { new CreateProductValidator() });

        return await validationBehavior.Handle(command, _ => handler.Handle(command, default), default);
    }

    [Fact]
    public async Task CreateProduct_With_A_Non_Url_ImageUrl_Is_Rejected_With_400_Before_Persisting()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var brand = new Brand { Name = "QA Brand", Description = "QA" };
        var category = new Category { Name = "QA Category", Description = "QA" };
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var act = async () => await TryCreateProductWithImageAsync(context, brand.Id, category.Id, "abc");

        // 7.8.4 regression: the real FluentValidation pipeline now rejects
        // this, via the exact same ValidationException contract every other
        // rule in this validator already uses (Color/Size) — same 400
        // shape, nothing new introduced.
        await act.Should().ThrowAsync<ValidationException>();

        (await context.ProductImages.AsNoTracking().AnyAsync())
            .Should().BeFalse("a rejected request must never reach SaveChangesAsync");
    }

    [Fact]
    public async Task CreateProduct_With_An_Empty_String_ImageUrl_Is_Rejected()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var brand = new Brand { Name = "QA Brand", Description = "QA" };
        var category = new Category { Name = "QA Category", Description = "QA" };
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var act = async () => await TryCreateProductWithImageAsync(context, brand.Id, category.Id, "");

        await act.Should().ThrowAsync<ValidationException>(
            "an empty string is not an absolute http/https URL either — Uri.TryCreate rejects it naturally, with no separate NotEmpty rule needed");
    }

    [Fact]
    public async Task CreateProduct_With_A_Valid_Url_Still_Works_Normally()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var brand = new Brand { Name = "QA Brand", Description = "QA" };
        var category = new Category { Name = "QA Category", Description = "QA" };
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        const string validUrl = "https://cdn.example.com/products/qa-image.jpg";

        var productId = await TryCreateProductWithImageAsync(context, brand.Id, category.Id, validUrl);
        productId.Should().NotBeEmpty();

        var persisted = await context.ProductImages.AsNoTracking()
            .Where(i => i.ProductId == productId)
            .Select(i => i.ImageUrl)
            .ToListAsync();

        persisted.Should().ContainSingle();
        persisted[0].Should().Be(validUrl, "a real, valid absolute http/https URL must continue to work exactly as before");
    }

    [Fact]
    public async Task CreateCategory_With_A_Non_Url_ImageUrl_Is_Rejected_With_400_Before_Persisting()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var command = new CreateCategoryCommand
        {
            Category = new CreateCategoryDto
            {
                Name = "QA Category",
                Slug = $"qa-{Guid.NewGuid():N}",
                Description = "QA",
                ImageUrl = "not-a-url-at-all"
            }
        };

        var cache = new Mock<ICacheService>();
        var handler = new CreateCategoryCommandHandler(context, cache.Object);

        // 7.8.4 regression: CreateCategoryValidator now exists (it didn't
        // before this phase), so ValidationBehavior's resolved validator
        // list is no longer empty for this command.
        var validationBehavior = new ValidationBehavior<CreateCategoryCommand, Guid>(
            new IValidator<CreateCategoryCommand>[] { new CreateCategoryValidator() });

        var act = async () => await validationBehavior.Handle(command, _ => handler.Handle(command, default), default);

        await act.Should().ThrowAsync<ValidationException>();

        (await context.Categories.AsNoTracking().AnyAsync())
            .Should().BeFalse("a rejected request must never reach SaveChangesAsync");
    }

    [Fact]
    public async Task UpdateCategory_Overwriting_A_Valid_Url_With_A_Non_Url_Value_Is_Rejected_And_The_Original_Url_Survives()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        const string originalUrl = "https://cdn.example.com/categories/original.jpg";

        var category = new Category
        {
            Name = "QA Category",
            Slug = $"qa-{Guid.NewGuid():N}",
            Description = "QA",
            ImageUrl = originalUrl
        };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var command = new UpdateCategoryCommand
        {
            Id = category.Id,
            Category = new UpdateCategoryDto
            {
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                ImageUrl = "<script>alert(1)</script>"
            }
        };

        var cache = new Mock<ICacheService>();
        var handler = new UpdateCategoryCommandHandler(context, cache.Object);

        // 7.8.4 regression: UpdateCategoryValidator now exists.
        var validationBehavior = new ValidationBehavior<UpdateCategoryCommand, bool>(
            new IValidator<UpdateCategoryCommand>[] { new UpdateCategoryValidator() });

        var act = async () => await validationBehavior.Handle(command, _ => handler.Handle(command, default), default);

        await act.Should().ThrowAsync<ValidationException>();

        var persisted = await context.Categories.AsNoTracking()
            .Where(c => c.Id == category.Id)
            .Select(c => c.ImageUrl)
            .SingleAsync();

        persisted.Should().Be(originalUrl, "a rejected update must never overwrite the previously valid URL");
    }

    // 7.8.4 policy decision (asked explicitly, not assumed): Category.ImageUrl
    // has no NotEmpty requirement anywhere in the existing code, and
    // CreateCategoryDto/UpdateCategoryDto both default to "". Rejecting
    // empty would have made every category-without-an-image request fail,
    // and would have broken the exact partial-update pattern already used
    // in CacheInvalidationTests.cs (updating Name/Description without
    // touching ImageUrl). The chosen policy: empty is a valid "no image"
    // state; only a NON-empty value must look like a URL.

    [Fact]
    public async Task CreateCategory_With_An_Empty_ImageUrl_Is_Allowed()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var command = new CreateCategoryCommand
        {
            Category = new CreateCategoryDto
            {
                Name = "QA Category",
                Slug = $"qa-{Guid.NewGuid():N}",
                Description = "QA",
                ImageUrl = ""
            }
        };

        var cache = new Mock<ICacheService>();
        var handler = new CreateCategoryCommandHandler(context, cache.Object);

        var validationBehavior = new ValidationBehavior<CreateCategoryCommand, Guid>(
            new IValidator<CreateCategoryCommand>[] { new CreateCategoryValidator() });

        var categoryId = await validationBehavior.Handle(command, _ => handler.Handle(command, default), default);

        categoryId.Should().NotBeEmpty("a category with no image at all is a valid state and must not be rejected");
    }

    [Fact]
    public async Task UpdateCategory_Without_Touching_ImageUrl_Still_Succeeds()
    {
        var (context, connection) = SqliteDbContextHelper.CreateDbContext();
        using var _ = connection;

        var category = new Category { Name = "QA Category", Slug = $"qa-{Guid.NewGuid():N}", Description = "QA" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        // Mirrors CacheInvalidationTests.cs's real-world pattern: a client
        // updating Name/Description without setting ImageUrl at all, which
        // leaves it at UpdateCategoryDto's default "".
        var command = new UpdateCategoryCommand
        {
            Id = category.Id,
            Category = new UpdateCategoryDto { Name = "Updated Name", Description = "x" }
        };

        var cache = new Mock<ICacheService>();
        var handler = new UpdateCategoryCommandHandler(context, cache.Object);

        var validationBehavior = new ValidationBehavior<UpdateCategoryCommand, bool>(
            new IValidator<UpdateCategoryCommand>[] { new UpdateCategoryValidator() });

        var succeeded = await validationBehavior.Handle(command, _ => handler.Handle(command, default), default);

        succeeded.Should().BeTrue("a partial update that never touches ImageUrl must not be rejected just because ImageUrl defaults to empty");
    }
}
