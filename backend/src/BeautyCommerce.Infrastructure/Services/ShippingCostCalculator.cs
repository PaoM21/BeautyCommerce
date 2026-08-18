using System.Globalization;
using System.Text;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using Microsoft.Extensions.Options;

namespace BeautyCommerce.Infrastructure.Services;

public class ShippingCostCalculator : IShippingCostCalculator
{
    private readonly ShippingRatesOptions _options;

    public ShippingCostCalculator(IOptions<ShippingRatesOptions> options)
    {
        _options = options.Value;
    }

    public decimal Calculate(string city)
    {
        return IsBogota(city) ? _options.BogotaCost : _options.NationalCost;
    }

    private static bool IsBogota(string city)
    {
        var normalized = RemoveDiacritics(city).Trim().ToLowerInvariant();

        return normalized.StartsWith("bogota", StringComparison.Ordinal);
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);

            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
