using BeautyCommerce.Application.Features.Categories.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public UpdateCategoryDto Category { get; set; } = new();
}