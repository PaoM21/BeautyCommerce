using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Entities;
using MediatR;

namespace BeautyCommerce.Application.Features.Categories.Commands.CreateCategory;

public class CreateCategoryCommandHandler
    : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public CreateCategoryCommandHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Guid> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Name = request.Category.Name,
            Slug = request.Category.Slug,
            Description = request.Category.Description,
            ImageUrl = request.Category.ImageUrl,
            ParentCategoryId = request.Category.ParentCategoryId
        };

        await _context.Categories.AddAsync(category, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await _cache.InvalidateTagAsync("Categories");

        return category.Id;
    }
}