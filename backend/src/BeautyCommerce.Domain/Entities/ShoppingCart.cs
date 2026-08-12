using BeautyCommerce.Domain.Base;

namespace BeautyCommerce.Domain.Entities;

public class ShoppingCart : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public ICollection<ShoppingCartItem> Items { get; set; }
        = new List<ShoppingCartItem>();
}