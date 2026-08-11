using BeautyCommerce.Application.Features.Categories.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Categories.Queries.GetAllCategories;

public class GetAllCategoriesQuery : IRequest<List<CategoryDto>>
{
}