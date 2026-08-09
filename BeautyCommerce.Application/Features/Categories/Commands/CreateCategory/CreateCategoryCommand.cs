using BeautyCommerce.Application.Features.Categories.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Categories.Commands.CreateCategory;

public class CreateCategoryCommand : IRequest<Guid>
{
    public CreateCategoryDto Category { get; set; } = new();
}