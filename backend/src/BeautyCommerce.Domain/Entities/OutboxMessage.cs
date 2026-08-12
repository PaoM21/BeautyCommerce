namespace BeautyCommerce.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; }

    public DateTime OccurredOn { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime? ProcessedOn { get; set; }

    public string? Error { get; set; }
}
