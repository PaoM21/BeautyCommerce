using BeautyCommerce.Application.Common.Constants;
using BeautyCommerce.Application.Features.Media.Commands.SyncImagesFromDrive;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyCommerce.API.Controllers;

[Authorize(Roles = Roles.Admin)]
[ApiController]
[Route("api/[controller]")]
public class MediaController : ControllerBase
{
    private readonly IMediator _mediator;

    public MediaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Sincroniza las imágenes de la carpeta de Google Drive configurada
    /// (GoogleDrive:FolderId) hacia Cloudinary, emparejándolas con
    /// productos por SKU (nombre de archivo = SKU, con sufijo "-N"
    /// opcional para fotos adicionales del mismo producto).
    /// </summary>
    [HttpPost("sync-drive-images")]
    public async Task<IActionResult> SyncDriveImages()
    {
        var result = await _mediator.Send(new SyncImagesFromDriveCommand());

        return Ok(result);
    }
}
