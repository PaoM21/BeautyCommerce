using MediatR;

namespace BeautyCommerce.Application.Features.Brands.Commands.DeleteBrand;

public class DeleteBrandCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}