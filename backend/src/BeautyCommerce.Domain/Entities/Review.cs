using BeautyCommerce.Domain.Base;

namespace BeautyCommerce.Domain.Entities;

public class Review : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public Guid? OrderId { get; set; }

    public Order? Order { get; set; }

    public int Rating { get; set; }

    public string Comment { get; set; } = string.Empty;
}
