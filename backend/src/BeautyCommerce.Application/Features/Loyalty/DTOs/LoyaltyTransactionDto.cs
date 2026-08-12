namespace BeautyCommerce.Application.Features.Loyalty.DTOs;

public class LoyaltyTransactionDto
{
    public Guid Id { get; set; }

    public int Points { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid? OrderId { get; set; }

    public DateTime CreatedAt { get; set; }
}
