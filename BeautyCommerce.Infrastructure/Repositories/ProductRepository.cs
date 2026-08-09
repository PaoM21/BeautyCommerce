using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BeautyCommerce.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Product?> GetWithDetailsAsync(Guid id)
    {
        return await _context.Products
            .Include(x => x.Brand)
            .Include(x => x.Category)
            .Include(x => x.Images)
            .Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public IQueryable<Product> Query()
    {
        return _context.Products.AsQueryable();
    }

    public async Task<List<Product>> GetFeaturedAsync()
    {
        return await _context.Products
            .Take(8)
            .ToListAsync();
    }
}
