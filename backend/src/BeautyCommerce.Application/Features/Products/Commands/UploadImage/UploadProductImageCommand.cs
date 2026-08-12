using MediatR;
using Microsoft.AspNetCore.Http;

namespace BeautyCommerce.Application.Features.Products.Commands.UploadImage;

public class UploadProductImageCommand
    : IRequest<Guid>
{
    public Guid ProductId { get; set; }

    public IFormFile File { get; set; } = null!;

    public bool IsPrimary { get; set; }
}