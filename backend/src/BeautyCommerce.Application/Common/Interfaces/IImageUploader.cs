namespace BeautyCommerce.Application.Common.Interfaces;

public interface IImageUploader
{
    /// <summary>
    /// Sube el contenido de una imagen al almacenamiento definitivo
    /// (Cloudinary) y devuelve la URL segura (https) resultante.
    /// </summary>
    Task<string> UploadAsync(
        Stream content,
        string publicId,
        CancellationToken cancellationToken = default);
}
