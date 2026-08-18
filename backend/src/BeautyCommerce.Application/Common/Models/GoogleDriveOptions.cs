namespace BeautyCommerce.Application.Common.Models;

/// <summary>
/// FolderId no es secreto y puede vivir en appsettings.json.
/// ServiceAccountJson (el contenido completo del JSON de la cuenta de
/// servicio de Google Cloud) es un secreto: debe configurarse solo vía
/// `dotnet user-secrets` en desarrollo o variables de entorno en
/// producción — nunca en un archivo versionado en git.
/// </summary>
public class GoogleDriveOptions
{
    public string FolderId { get; set; } = string.Empty;

    public string ServiceAccountJson { get; set; } = string.Empty;
}
