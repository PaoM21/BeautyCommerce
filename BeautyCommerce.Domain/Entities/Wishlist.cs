using BeautyCommerce.Domain.Base;

namespace BeautyCommerce.Domain.Entities;

public class Wishlist : BaseEntity
{
    public Guid UserId { get; set; }

    public ICollection<WishlistItem> Items { get; set; }
        = new List<WishlistItem>();
}