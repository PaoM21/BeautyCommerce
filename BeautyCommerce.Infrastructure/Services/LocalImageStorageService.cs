using BeautyCommerce.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace BeautyCommerce.Infrastructure.Services;

public class LocalImageStorageService : IImageStorageService
{
    private readonly IWebHostEnvironment _environment;

    public LocalImageStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveProductImageAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName);

        var newName =
            $"{Guid.NewGuid()}{extension}";

        var folder = Path.Combine(
            _environment.WebRootPath,
            "images",
            "products");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var path = Path.Combine(folder, newName);

        using var file = File.Create(path);

        await stream.CopyToAsync(file, cancellationToken);

        return $"/images/products/{newName}";
    }
}