using BeautyCommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Application.Features.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommandHandler
    : IRequestHandler<UpdateCategoryCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (category == null)
            return false;

        category.Name = request.Category.Name;
        category.Slug = request.Category.Slug;
        category.Description = request.Category.Description;
        category.ImageUrl = request.Category.ImageUrl;
        category.ParentCategoryId = request.Category.ParentCategoryId;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}