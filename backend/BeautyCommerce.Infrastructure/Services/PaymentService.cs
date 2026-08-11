using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace BeautyCommerce.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ILogger<PaymentService> logger)
    {
        _logger = logger;
    }

    public Task<PaymentResult> CreatePaymentAsync(
        decimal amount,
        string currency,
        string description)
    {
        _logger.LogInformation(
            "Processing simulated payment. Amount: {Amount} {Currency}, Description: {Description}",
            amount,
            currency,
            description);

        if (amount <= 0)
        {
            return Task.FromResult(
                PaymentResult.Failed(
                    "El valor del pago debe ser mayor que cero."));
        }

        var transactionId =
            $"SIM-{Guid.NewGuid():N}";

        _logger.LogInformation(
            "Simulated payment successful. TransactionId: {TransactionId}",
            transactionId);

        return Task.FromResult(
            PaymentResult.Succeeded(transactionId));
    }
}
