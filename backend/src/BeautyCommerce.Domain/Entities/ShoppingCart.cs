using BeautyCommerce.Domain.Base;

namespace BeautyCommerce.Domain.Entities;

public class ShoppingCart : BaseEntity
{
    public Guid UserId { get; set; }

    public ICollection<ShoppingCartItem> Items { get; set; }
        = new List<ShoppingCartItem>();
}