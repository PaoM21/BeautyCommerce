using BeautyCommerce.Application.Features.Products.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Products.Queries.GetProductById;

public class GetProductByIdQuery : IRequest<ProductDetailDto>
{
    public Guid Id { get; set; }
}