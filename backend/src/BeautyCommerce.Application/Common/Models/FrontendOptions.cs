namespace BeautyCommerce.Application.Common.Models;

/// <summary>
/// URL base del frontend público, usada para construir enlaces dentro
/// de los correos (ej. el link de restablecer contraseña).
/// </summary>
public class FrontendOptions
{
    public string BaseUrl { get; set; } = string.Empty;
}
