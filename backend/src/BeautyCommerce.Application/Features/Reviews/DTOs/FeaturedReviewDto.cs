namespace BeautyCommerce.Application.Features.Reviews.DTOs;

public class FeaturedReviewDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;
}
