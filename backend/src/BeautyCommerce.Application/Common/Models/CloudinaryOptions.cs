namespace BeautyCommerce.Application.Common.Models;

/// <summary>
/// CloudName no es secreto. ApiKey y ApiSecret sí lo son: deben
/// configurarse solo vía `dotnet user-secrets` en desarrollo o
/// variables de entorno en producción — nunca en un archivo
/// versionado en git.
/// </summary>
public class CloudinaryOptions
{
    public string CloudName { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>
    /// Carpeta/prefijo dentro de Cloudinary donde se suben las
    /// imágenes sincronizadas, para no mezclarlas con otros usos futuros.
    /// </summary>
    public string Folder { get; set; } = "beautycommerce/products";
}
