using BeautyCommerce.Application.Common.Interfaces;

namespace BeautyCommerce.Tests.Helpers;

/// <summary>
/// Stub usado en pruebas que no están enfocadas en el cálculo del costo
/// de envío, para no repetir el mismo mock de Moq en cada una.
/// </summary>
public class ZeroShippingCostCalculator : IShippingCostCalculator
{
    public decimal Calculate(string city) => 0m;
}
