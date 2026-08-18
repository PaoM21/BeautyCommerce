using BeautyCommerce.Domain.Base;

namespace BeautyCommerce.Domain.Entities;

public class NewsletterSubscriber : BaseEntity
{
    public string Email { get; set; } = string.Empty;
}
