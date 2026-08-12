using BeautyCommerce.Application.Features.Categories.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Categories.Queries.GetCategoryById;

public class GetCategoryByIdQuery : IRequest<CategoryDto?>
{
    public Guid Id { get; set; }
}