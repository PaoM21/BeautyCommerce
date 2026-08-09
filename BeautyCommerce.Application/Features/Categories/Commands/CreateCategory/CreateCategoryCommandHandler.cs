using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Entities;
using MediatR;

namespace BeautyCommerce.Application.Features.Categories.Commands.CreateCategory;

public class CreateCategoryCommandHandler
    : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
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

        return category.Id;
    }
}