namespace BeautyCommerce.Application.Features.Reviews.DTOs;

public class ReviewDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
