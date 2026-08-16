using System.Security.Cryptography;
using BeautyCommerce.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Infrastructure.Services;

public class ProductVariantIdentifierGenerator : IProductVariantIdentifierGenerator
{
    private const int MaxAttempts = 10;

    private readonly IApplicationDbContext _context;

    public ProductVariantIdentifierGenerator(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task<string> GenerateSkuAsync(CancellationToken cancellationToken = default) =>
        GenerateUniqueAsync(
            "SKU-",
            candidate => _context.ProductVariants
                .IgnoreQueryFilters()
                .AnyAsync(v => v.SKU == candidate, cancellationToken));

    public Task<string> GenerateBarcodeAsync(CancellationToken cancellationToken = default) =>
        GenerateUniqueAsync(
            "BC-",
            candidate => _context.ProductVariants
                .IgnoreQueryFilters()
                .AnyAsync(v => v.Barcode == candidate, cancellationToken));

    // IgnoreQueryFilters: uniqueness is global, not scoped to active rows —
    // a soft-deleted variant still occupies its SKU/Barcode forever, same
    // guarantee the UNIQUE index in PostgreSQL will enforce.
    private static async Task<string> GenerateUniqueAsync(
        string prefix,
        Func<string, Task<bool>> existsAsync)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var candidate = prefix + RandomNumberGenerator.GetHexString(8, lowercase: false);

            if (!await existsAsync(candidate))
                return candidate;
        }

        throw new InvalidOperationException(
            $"No se pudo generar un identificador único con el prefijo '{prefix}' tras {MaxAttempts} intentos.");
    }
}
