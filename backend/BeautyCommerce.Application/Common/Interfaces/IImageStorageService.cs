namespace BeautyCommerce.Application.Common.Interfaces;

public interface IImageStorageService
{
    Task<string> SaveProductImageAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken);
}