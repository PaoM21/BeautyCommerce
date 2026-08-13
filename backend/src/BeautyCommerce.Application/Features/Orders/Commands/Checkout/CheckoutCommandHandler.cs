using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Application.Common.Models;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BeautyCommerce.Application.Features.Orders.Commands.Checkout;

public class CheckoutCommandHandler
    : IRequestHandler<CheckoutCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IPaymentService _paymentService;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<CheckoutCommandHandler> _logger;

    public CheckoutCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IPaymentService paymentService,
        IInventoryService inventoryService,
        ILogger<CheckoutCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _paymentService = paymentService;
        _inventoryService = inventoryService;
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
            if (item.ProductVariant == null)
            {
                throw new BadRequestException(
                    $"La variante {item.ProductVariantId} no existe.");
            }

            if (item.Quantity <= 0)
            {
                throw new BadRequestException(
                    "La cantidad de un producto debe ser mayor que cero.");
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

        var orderNumber = $"ORD-{Guid.NewGuid():N}";

        // Reserve stock atomically for every item BEFORE charging the
        // customer, and before this handler creates anything of its own.
        // IInventoryService owns the only atomic stock decrement in the
        // system (see InventoryService.TryRegisterExitAsync) and records
        // the InventoryMovement itself, so checkout no longer touches
        // ProductVariant.Stock or InventoryMovement directly. If any item
        // can't be reserved, we throw here — nothing has been charged yet,
        // and TransactionBehavior rolls back any reservations already made
        // for earlier items in this same loop.
        foreach (var item in cart.Items)
        {
            var reserved = await _inventoryService.TryRegisterExitAsync(
                item.ProductVariantId,
                item.Quantity,
                $"Salida por pedido {orderNumber}",
                userId,
                cancellationToken);

            if (!reserved)
            {
                throw new BadRequestException(
                    $"No hay stock suficiente para la variante {item.ProductVariantId}.");
            }
        }

        /*
         * Pago simulado.
         *
         * Cuando integremos una pasarela real,
         * este servicio será reemplazado por la implementación
         * correspondiente.
         */
        var payment = await _paymentService.CreatePaymentAsync(
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
                payment.Message ??
                "No fue posible procesar el pago.");
        }

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

        _context.ShoppingCartItems.RemoveRange(cart.Items);

        /*
         * PERSISTIR TODO:
         * - Order
         * - OrderItems
         * - OutboxMessage
         * - ShoppingCartItems eliminados
         *
         * El stock y su InventoryMovement ya se persistieron arriba, uno
         * por variante, dentro de TryRegisterExitAsync — pero siguen
         * dentro de esta misma transacción (TransactionBehavior), así que
         * si algo de lo anterior falla aquí, también se revierte.
         */
        await _context.SaveChangesAsync(cancellationToken);

        // Loyalty awarding is handled asynchronously by the OutboxProcessor
        // to keep checkout responsibilities focused and decoupled.

        _logger.LogInformation(
            "Order created successfully. OrderId {OrderId}, UserId {UserId}, Total {Total}",
            order.Id,
            userId,
            total);

        return order.Id;
    }
}
