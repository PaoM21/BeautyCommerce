using BeautyCommerce.Domain.Base;

namespace BeautyCommerce.Domain.Entities;

public class LoyaltyTransaction : BaseEntity
{
    public Guid LoyaltyAccountId { get; set; }

    public LoyaltyAccount LoyaltyAccount { get; set; } = null!;

    public int Points { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid? OrderId { get; set; }

    public Order? Order { get; set; }
}
