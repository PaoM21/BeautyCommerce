using BeautyCommerce.Application.Features.Products.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Products.Commands.CreateProduct;

public class CreateProductCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid BrandId { get; set; }

    public Guid CategoryId { get; set; }

    public bool IsFeatured { get; set; }

    public List<CreateVariantDto> Variants { get; set; } = [];

    public List<string> Images { get; set; } = [];
}