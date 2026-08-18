namespace BeautyCommerce.Application.Common.Interfaces;

public interface IShippingCostCalculator
{
    /// <summary>
    /// Calcula el costo de envío según la ciudad de destino.
    /// </summary>
    decimal Calculate(string city);
}
