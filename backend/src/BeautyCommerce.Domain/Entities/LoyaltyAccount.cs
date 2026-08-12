using BeautyCommerce.Domain.Base;

namespace BeautyCommerce.Domain.Entities;

public class LoyaltyAccount : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public int Points { get; set; }

    public string Level { get; set; } = "Bronze";

    public ICollection<LoyaltyTransaction> Transactions { get; set; } = new List<LoyaltyTransaction>();
}
