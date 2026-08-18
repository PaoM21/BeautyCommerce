using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Options;

namespace BeautyCommerce.Infrastructure.Services;

public class GoogleDriveFileProvider : IDriveFileProvider
{
    private readonly GoogleDriveOptions _options;

    public GoogleDriveFileProvider(IOptions<GoogleDriveOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IReadOnlyList<DriveFileInfo>> ListImageFilesAsync(
        string folderId,
        CancellationToken cancellationToken = default)
    {
        using var service = CreateService();

        var request = service.Files.List();
        request.Q = $"'{folderId}' in parents and trashed = false";
        request.Fields = "files(id, name, mimeType)";
        request.PageSize = 1000;

        var response = await request.ExecuteAsync(cancellationToken);

        return response.Files
            .Select(f => new DriveFileInfo(f.Id, f.Name, f.MimeType ?? string.Empty))
            .ToList();
    }

    public async Task<Stream> DownloadFileAsync(
        string fileId,
        CancellationToken cancellationToken = default)
    {
        using var service = CreateService();

        var memoryStream = new MemoryStream();

        await service.Files.Get(fileId).DownloadAsync(memoryStream, cancellationToken);

        memoryStream.Position = 0;

        return memoryStream;
    }

    private DriveService CreateService()
    {
        if (string.IsNullOrWhiteSpace(_options.ServiceAccountJson))
        {
            throw new InvalidOperationException(
                "GoogleDrive:ServiceAccountJson no está configurado. Ver docs/GOOGLE_DRIVE_IMAGE_SYNC.md.");
        }

        // GoogleCredential.FromJson está marcado obsoleto a favor de
        // CredentialFactory, que todavía no es una API pública estable
        // en la versión del SDK usada aquí. FromJson sigue siendo el
        // mecanismo soportado para credenciales de cuenta de servicio
        // desde un JSON en memoria.
#pragma warning disable CS0618
        var credential = GoogleCredential.FromJson(_options.ServiceAccountJson)
            .CreateScoped(DriveService.ScopeConstants.DriveReadonly);
#pragma warning restore CS0618

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "BeautyCommerce",
        });
    }
}
