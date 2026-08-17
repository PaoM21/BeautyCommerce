using BeautyCommerce.Application.Common.Models;
using MediatR;

namespace BeautyCommerce.Application.Features.Media.Commands.SyncImagesFromDrive;

public class SyncImagesFromDriveCommand : IRequest<DriveSyncResult>
{
}
