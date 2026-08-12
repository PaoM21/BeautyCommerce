using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Features.Categories.DTOs;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace BeautyCommerce.Application.Features.Categories.Queries.GetAllCategories;

public class GetAllCategoriesQueryHandler
    : IRequestHandler<GetAllCategoriesQuery, List<CategoryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAllCategoriesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CategoryDto>> Handle(
        GetAllCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Categories
            .Select(x => new CategoryDto
            {
                Id = x.Id,
                Name = x.Name,
                Slug = x.Slug,
                Description = x.Description,
                ImageUrl = x.ImageUrl,
                ParentCategoryId = x.ParentCategoryId,
                IsActive = x.IsActive
            })
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}