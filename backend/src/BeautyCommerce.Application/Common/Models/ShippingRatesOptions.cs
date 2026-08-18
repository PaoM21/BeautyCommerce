namespace BeautyCommerce.Application.Common.Models;

/// <summary>
/// Tarifas de envío en pesos colombianos. Son una decisión de negocio,
/// no técnica — los valores por defecto en appsettings.json son
/// placeholders y deben confirmarse/ajustarse antes de salir a
/// producción con clientes reales.
/// </summary>
public class ShippingRatesOptions
{
    public decimal BogotaCost { get; set; }

    public decimal NationalCost { get; set; }
}
