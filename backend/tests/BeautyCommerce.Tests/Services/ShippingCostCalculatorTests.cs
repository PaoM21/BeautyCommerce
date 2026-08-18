using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace BeautyCommerce.Tests.Services;

public class ShippingCostCalculatorTests
{
    private static ShippingCostCalculator CreateCalculator()
    {
        return new ShippingCostCalculator(
            Options.Create(new ShippingRatesOptions
            {
                BogotaCost = 8000m,
                NationalCost = 15000m,
            }));
    }

    [Theory]
    [InlineData("Bogotá")]
    [InlineData("bogota")]
    [InlineData("BOGOTÁ")]
    [InlineData("Bogotá D.C.")]
    [InlineData("  Bogotá  ")]
    public void Should_Return_Bogota_Cost_For_Bogota_Variants(string city)
    {
        var calculator = CreateCalculator();

        calculator.Calculate(city).Should().Be(8000m);
    }

    [Theory]
    [InlineData("Medellín")]
    [InlineData("Cali")]
    [InlineData("Barranquilla")]
    [InlineData("")]
    public void Should_Return_National_Cost_For_Other_Cities(string city)
    {
        var calculator = CreateCalculator();

        calculator.Calculate(city).Should().Be(15000m);
    }
}
