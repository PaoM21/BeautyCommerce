using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BeautyCommerce.Application.Features.Orders.Commands.Checkout;

public class CheckoutCommandHandler
    : IRequestHandler<CheckoutCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<CheckoutCommandHandler> _logger;

    public CheckoutCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IPaymentService paymentService,
        ILogger<CheckoutCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<Guid> Handle(
        CheckoutCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
            throw new UnauthorizedAccessException();

        var userId = _currentUser.UserId.Value;

        _logger.LogInformation(
            "Starting checkout for user {UserId}",
            userId);

        try
        {
            var cart = await _context.ShoppingCarts
                .Include(x => x.Items)
                .ThenInclude(i => i.ProductVariant)
                .FirstOrDefaultAsync(
                    x => x.UserId == userId,
                    cancellationToken);

            if (cart == null || !cart.Items.Any())
                throw new BadRequestException(
                    "El carrito está vacío.");

            foreach (var item in cart.Items)
            {
                if (item.ProductVariant.Stock < item.Quantity)
                {
                    throw new BadRequestException(
                        $"No hay stock suficiente para la variante {item.ProductVariantId}.");
                }
            }

            var subTotal = cart.Items.Sum(
                item => item.Quantity * item.UnitPrice);

            var shippingCost = 0m;

            var tax = 0m;

            var total = subTotal + shippingCost + tax;

            if (total <= 0)
                throw new BadRequestException(
                    "El total del pedido debe ser mayor que cero.");

            var orderNumber =
                $"ORD-{Guid.NewGuid():N}";

            var payment =
                await _paymentService.CreatePaymentAsync(
                    total,
                    "COP",
                    $"Pedido {orderNumber}");

            if (!payment.Success)
            {
                _logger.LogWarning(
                    "Payment failed for user {UserId}: {Message}",
                    userId,
                    payment.Message);

                throw new BadRequestException(
                    payment.Message ?? "No fue posible procesar el pago.");
            }

            _logger.LogInformation(
                "Payment succeeded for user {UserId}. " +
                "OrderNumber {OrderNumber}. Transaction {Transaction}",
                userId,
                orderNumber,
                payment.TransactionId);

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,

                Status = OrderStatus.Paid,

                SubTotal = subTotal,
                ShippingCost = shippingCost,
                Tax = tax,
                Total = total,
                OrderNumber = orderNumber,
                TransactionId = payment.TransactionId
            };

            _context.Orders.Add(order);

            foreach (var item in cart.Items)
            {
                var orderItem = new OrderItem
                {
                    Order = order,
                    ProductVariantId = item.ProductVariantId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                };

                _context.OrderItems.Add(orderItem);

                item.ProductVariant.Stock -= item.Quantity;

                _logger.LogInformation(
                    "Inventory changed for Variant {VariantId}: " +
                    "-{Quantity}, NewStock {Stock}",
                    item.ProductVariantId,
                    item.Quantity,
                    item.ProductVariant.Stock);
            }

            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccurredOn = DateTime.UtcNow,
                Type = "OrderCreated",
                Content = JsonSerializer.Serialize(new
                {
                    order.Id,
                    order.OrderNumber,
                    order.Total,
                    order.UserId
                })
            };

            _context.OutboxMessages.Add(message);

            _context.ShoppingCartItems.RemoveRange(
                cart.Items);

            _logger.LogInformation(
                "Order created successfully. " +
                "OrderId {OrderId}, UserId {UserId}, Total {Total}",
                order.Id,
                userId,
                total);

            return order.Id;
        }
        catch (Exception ex)
        {

            _logger.LogError(
                ex,
                "Error processing checkout for user {UserId}",
                userId);

            throw;
        }
    }
}
