using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Application.Features.Media.Commands.SyncImagesFromDrive;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace BeautyCommerce.Tests.Commands.Media;

public class SyncImagesFromDriveCommandHandlerTests
{
    private static Product CreateProductWithVariant(string sku)
    {
        var product = new Product
        {
            Name = "Labial Mate Rojo",
            Slug = "labial-mate-rojo",
        };

        product.Variants.Add(new ProductVariant
        {
            SKU = sku,
            Color = "Rojo",
            Size = "Único",
        });

        return product;
    }

    private static SyncImagesFromDriveCommandHandler CreateHandler(
        BeautyCommerce.Infrastructure.Persistence.ApplicationDbContext context,
        Mock<IDriveFileProvider> driveMock,
        Mock<IImageUploader> uploaderMock,
        string folderId = "folder-123")
    {
        var options = Options.Create(new GoogleDriveOptions { FolderId = folderId });

        return new SyncImagesFromDriveCommandHandler(
            context,
            driveMock.Object,
            uploaderMock.Object,
            options,
            NullLogger<SyncImagesFromDriveCommandHandler>.Instance);
    }

    [Fact]
    public async Task Should_Match_File_By_Sku_As_Primary_Image()
    {
        // Arrange
        var context = DbContextHelper.CreateDbContext();
        var product = CreateProductWithVariant("SKU1");
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync(default);
        var productId = product.Id;

        var driveMock = new Mock<IDriveFileProvider>();
        driveMock
            .Setup(x => x.ListImageFilesAsync("folder-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriveFileInfo>
            {
                new("file-1", "SKU1.jpg", "image/jpeg"),
            });
        driveMock
            .Setup(x => x.DownloadFileAsync("file-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(new byte[] { 1, 2, 3 }));

        var uploaderMock = new Mock<IImageUploader>();
        uploaderMock
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), "SKU1-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://res.cloudinary.com/demo/sku1-1.jpg");

        var handler = CreateHandler(context, driveMock, uploaderMock);

        // Act
        var result = await handler.Handle(new SyncImagesFromDriveCommand(), default);

        // Assert
        result.FilesProcessed.Should().Be(1);
        result.UnmatchedFiles.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
        result.UpdatedProducts.Should().ContainSingle();
        result.UpdatedProducts[0].ProductId.Should().Be(productId);
        result.UpdatedProducts[0].IsPrimary.Should().BeTrue();

        var image = context.ProductImages.Single();
        image.ProductId.Should().Be(productId);
        image.ImageUrl.Should().Be("https://res.cloudinary.com/demo/sku1-1.jpg");
        image.IsPrimary.Should().BeTrue();
        image.DisplayOrder.Should().Be(1);
    }

    [Fact]
    public async Task Should_Match_Suffixed_File_As_Secondary_Non_Primary_Image()
    {
        // Arrange
        var context = DbContextHelper.CreateDbContext();
        var product = CreateProductWithVariant("SKU1");
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync(default);
        var productId = product.Id;

        var driveMock = new Mock<IDriveFileProvider>();
        driveMock
            .Setup(x => x.ListImageFilesAsync("folder-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriveFileInfo>
            {
                new("file-2", "SKU1-2.png", "image/png"),
            });
        driveMock
            .Setup(x => x.DownloadFileAsync("file-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(new byte[] { 1, 2, 3 }));

        var uploaderMock = new Mock<IImageUploader>();
        uploaderMock
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), "SKU1-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://res.cloudinary.com/demo/sku1-2.jpg");

        var handler = CreateHandler(context, driveMock, uploaderMock);

        // Act
        var result = await handler.Handle(new SyncImagesFromDriveCommand(), default);

        // Assert
        var image = context.ProductImages.Single();
        image.ProductId.Should().Be(productId);
        image.DisplayOrder.Should().Be(2);
        image.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Report_Unmatched_File_When_No_Variant_Sku_Matches()
    {
        // Arrange
        var context = DbContextHelper.CreateDbContext();

        var driveMock = new Mock<IDriveFileProvider>();
        driveMock
            .Setup(x => x.ListImageFilesAsync("folder-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriveFileInfo>
            {
                new("file-3", "SKU-DESCONOCIDO.jpg", "image/jpeg"),
            });

        var uploaderMock = new Mock<IImageUploader>();

        var handler = CreateHandler(context, driveMock, uploaderMock);

        // Act
        var result = await handler.Handle(new SyncImagesFromDriveCommand(), default);

        // Assert
        result.UnmatchedFiles.Should().ContainSingle().Which.Should().Be("SKU-DESCONOCIDO.jpg");
        result.UpdatedProducts.Should().BeEmpty();
        context.ProductImages.Should().BeEmpty();

        uploaderMock.Verify(
            x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Skip_Non_Image_Files()
    {
        // Arrange
        var context = DbContextHelper.CreateDbContext();
        var product = CreateProductWithVariant("SKU1");
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync(default);

        var driveMock = new Mock<IDriveFileProvider>();
        driveMock
            .Setup(x => x.ListImageFilesAsync("folder-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriveFileInfo>
            {
                new("file-4", "SKU1.pdf", "application/pdf"),
            });

        var uploaderMock = new Mock<IImageUploader>();

        var handler = CreateHandler(context, driveMock, uploaderMock);

        // Act
        var result = await handler.Handle(new SyncImagesFromDriveCommand(), default);

        // Assert
        result.FilesProcessed.Should().Be(0);
        result.UnmatchedFiles.Should().BeEmpty();
        context.ProductImages.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Update_Existing_Image_Instead_Of_Duplicating_On_Second_Sync()
    {
        // Arrange
        var context = DbContextHelper.CreateDbContext();
        var product = CreateProductWithVariant("SKU1");
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync(default);
        var productId = product.Id;

        var driveMock = new Mock<IDriveFileProvider>();
        driveMock
            .Setup(x => x.ListImageFilesAsync("folder-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriveFileInfo>
            {
                new("file-1", "SKU1.jpg", "image/jpeg"),
            });
        driveMock
            .Setup(x => x.DownloadFileAsync("file-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(new byte[] { 1, 2, 3 }));

        var uploaderMock = new Mock<IImageUploader>();
        uploaderMock
            .SetupSequence(x => x.UploadAsync(It.IsAny<Stream>(), "SKU1-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://res.cloudinary.com/demo/v1.jpg")
            .ReturnsAsync("https://res.cloudinary.com/demo/v2.jpg");

        // Act — sincroniza dos veces (simula correr el botón dos veces)
        var handler1 = CreateHandler(context, driveMock, uploaderMock);
        await handler1.Handle(new SyncImagesFromDriveCommand(), default);

        var handler2 = CreateHandler(context, driveMock, uploaderMock);
        await handler2.Handle(new SyncImagesFromDriveCommand(), default);

        // Assert — sigue habiendo una sola imagen para el producto, actualizada
        var images = context.ProductImages.Where(i => i.ProductId == productId).ToList();
        images.Should().ContainSingle();
        images[0].ImageUrl.Should().Be("https://res.cloudinary.com/demo/v2.jpg");
    }
}
