namespace BeautyCommerce.Application.Common.Interfaces;

public interface IProductVariantIdentifierGenerator
{
    Task<string> GenerateSkuAsync(CancellationToken cancellationToken = default);

    Task<string> GenerateBarcodeAsync(CancellationToken cancellationToken = default);
}
