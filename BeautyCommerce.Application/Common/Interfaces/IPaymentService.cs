namespace BeautyCommerce.Application.Common.Interfaces;

public interface IPaymentService
{
    Task<BeautyCommerce.Application.Common.Models.PaymentResult> CreatePaymentAsync(
        decimal amount,
        string currency,
        string description);
}
