using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace BeautyCommerce.Infrastructure.Services;

public class CloudinaryImageUploader : IImageUploader
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinaryOptions _options;

    public CloudinaryImageUploader(IOptions<CloudinaryOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.CloudName) ||
            string.IsNullOrWhiteSpace(_options.ApiKey) ||
            string.IsNullOrWhiteSpace(_options.ApiSecret))
        {
            throw new InvalidOperationException(
                "La configuración de Cloudinary (CloudName/ApiKey/ApiSecret) no está completa. Ver docs/GOOGLE_DRIVE_IMAGE_SYNC.md.");
        }

        var account = new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret);

        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadAsync(
        Stream content,
        string publicId,
        CancellationToken cancellationToken = default)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(publicId, content),
            PublicId = $"{_options.Folder}/{publicId}",
            Overwrite = true,
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken: cancellationToken);

        if (result.Error != null)
        {
            throw new InvalidOperationException(
                $"Error subiendo imagen a Cloudinary: {result.Error.Message}");
        }

        return result.SecureUrl.ToString();
    }
}
