using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BeautyCommerce.Application.Features.Media.Commands.SyncImagesFromDrive;

public class SyncImagesFromDriveCommandHandler
    : IRequestHandler<SyncImagesFromDriveCommand, DriveSyncResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IDriveFileProvider _driveFileProvider;
    private readonly IImageUploader _imageUploader;
    private readonly GoogleDriveOptions _options;
    private readonly ILogger<SyncImagesFromDriveCommandHandler> _logger;

    public SyncImagesFromDriveCommandHandler(
        IApplicationDbContext context,
        IDriveFileProvider driveFileProvider,
        IImageUploader imageUploader,
        IOptions<GoogleDriveOptions> options,
        ILogger<SyncImagesFromDriveCommandHandler> logger)
    {
        _context = context;
        _driveFileProvider = driveFileProvider;
        _imageUploader = imageUploader;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<DriveSyncResult> Handle(
        SyncImagesFromDriveCommand request,
        CancellationToken cancellationToken)
    {
        var result = new DriveSyncResult();

        if (string.IsNullOrWhiteSpace(_options.FolderId))
        {
            result.Errors.Add(
                "GoogleDrive:FolderId no está configurado.");

            return result;
        }

        var files = await _driveFileProvider.ListImageFilesAsync(
            _options.FolderId,
            cancellationToken);

        var imageFiles = files
            .Where(f => f.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var file in imageFiles)
        {
            result.FilesProcessed++;

            try
            {
                await ProcessFileAsync(file, result, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sincronizando la imagen {FileName} desde Drive",
                    file.Name);

                result.Errors.Add($"{file.Name}: {ex.Message}");
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return result;
    }

    private async Task ProcessFileAsync(
        DriveFileInfo file,
        DriveSyncResult result,
        CancellationToken cancellationToken)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);

        ProductVariant? variant = null;
        var matchedOrder = 1;

        foreach (var (sku, order) in DriveFileNameParser.GetCandidates(nameWithoutExtension))
        {
            variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(
                    v => v.SKU.ToLower() == sku.ToLower(),
                    cancellationToken);

            if (variant != null)
            {
                matchedOrder = order;
                break;
            }
        }

        if (variant == null)
        {
            result.UnmatchedFiles.Add(file.Name);
            return;
        }

        await using var content = await _driveFileProvider.DownloadFileAsync(
            file.Id,
            cancellationToken);

        var publicId = $"{variant.SKU}-{matchedOrder}";

        var imageUrl = await _imageUploader.UploadAsync(
            content,
            publicId,
            cancellationToken);

        var existingImage = await _context.ProductImages
            .FirstOrDefaultAsync(
                i => i.ProductId == variant.ProductId && i.DisplayOrder == matchedOrder,
                cancellationToken);

        var isPrimary = matchedOrder == 1;

        if (existingImage != null)
        {
            existingImage.ImageUrl = imageUrl;
            existingImage.IsPrimary = isPrimary;
        }
        else
        {
            await _context.ProductImages.AddAsync(
                new ProductImage
                {
                    ProductId = variant.ProductId,
                    ImageUrl = imageUrl,
                    DisplayOrder = matchedOrder,
                    IsPrimary = isPrimary,
                },
                cancellationToken);
        }

        if (isPrimary)
        {
            var otherImages = await _context.ProductImages
                .Where(i =>
                    i.ProductId == variant.ProductId &&
                    i.DisplayOrder != matchedOrder &&
                    i.IsPrimary)
                .ToListAsync(cancellationToken);

            foreach (var other in otherImages)
            {
                other.IsPrimary = false;
            }
        }

        result.UpdatedProducts.Add(new DriveSyncMatch
        {
            ProductId = variant.ProductId,
            ProductName = variant.Product.Name,
            SourceFile = file.Name,
            ImageUrl = imageUrl,
            IsPrimary = isPrimary,
        });
    }
}
