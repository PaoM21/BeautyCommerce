using BeautyCommerce.Domain.Entities;

namespace BeautyCommerce.Application.Common.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);

    Task<Product?> GetWithDetailsAsync(Guid id);

    Task<List<Product>> GetFeaturedAsync();

    IQueryable<Product> Query();
}
