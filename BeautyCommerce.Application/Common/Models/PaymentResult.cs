namespace BeautyCommerce.Application.Common.Models;

public class PaymentResult
{
    public bool Success { get; set; }

    public string? TransactionId { get; set; }

    public string? Message { get; set; }

    public static PaymentResult Succeeded(
        string transactionId)
    {
        return new PaymentResult
        {
            Success = true,
            TransactionId = transactionId,
            Message = "Pago procesado correctamente."
        };
    }

    public static PaymentResult Failed(
        string message)
    {
        return new PaymentResult
        {
            Success = false,
            Message = message
        };
    }
}
