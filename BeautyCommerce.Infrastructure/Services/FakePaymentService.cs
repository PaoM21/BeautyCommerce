using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;

namespace BeautyCommerce.Infrastructure.Services;

public class FakePaymentService : IPaymentService
{
    public async Task<PaymentResult> CreatePaymentAsync(
        decimal amount,
        string currency,
        string description)
    {
        await Task.Delay(500);

        var transactionId = Guid.NewGuid().ToString();

        return PaymentResult.Succeeded(transactionId);
    }
}
