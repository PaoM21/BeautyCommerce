namespace BeautyCommerce.Application.Common.Interfaces;

public record DriveFileInfo(string Id, string Name, string MimeType);

public interface IDriveFileProvider
{
    Task<IReadOnlyList<DriveFileInfo>> ListImageFilesAsync(
        string folderId,
        CancellationToken cancellationToken = default);

    Task<Stream> DownloadFileAsync(
        string fileId,
        CancellationToken cancellationToken = default);
}
