namespace BeautyCommerce.Application.Common.Models;

/// <summary>
/// FromAddress/FromName no son secretos. ApiKey sí lo es: debe
/// configurarse solo vía `dotnet user-secrets` en desarrollo o
/// variables de entorno en producción — nunca en un archivo
/// versionado en git.
/// </summary>
public class EmailOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = string.Empty;
}
