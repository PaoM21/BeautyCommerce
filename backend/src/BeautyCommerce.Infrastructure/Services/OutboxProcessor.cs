using System.Text.Json;
using BeautyCommerce.Application.Common.Emails;
using BeautyCommerce.Application.Common.Interfaces;
using BeautyCommerce.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using BeautyCommerce.Infrastructure.Persistence;

namespace BeautyCommerce.Infrastructure.Services;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                var messages = await db.OutboxMessages
                    .Where(m => m.ProcessedOn == null)
                    .OrderBy(m => m.OccurredOn)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var msg in messages)
                {
                    await ProcessMessageAsync(
                        db, emailService, userService, msg, _logger, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in outbox processing loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    /// <summary>
    /// Procesa un único mensaje del outbox. Público/estático para poder
    /// probarlo directamente en tests sin levantar el BackgroundService
    /// completo.
    /// </summary>
    public static async Task ProcessMessageAsync(
        ApplicationDbContext db,
        IEmailService emailService,
        IUserService userService,
        OutboxMessage message,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Processing outbox message {Id} Type={Type}", message.Id, message.Type);

            switch (message.Type)
            {
                case "OrderCreated":
                    await SendOrderConfirmationEmailAsync(
                        db, emailService, userService, message, cancellationToken);
                    break;

                case "OrderShipped":
                    await SendOrderShippedEmailAsync(
                        db, emailService, userService, message, cancellationToken);
                    break;

                default:
                    logger.LogWarning("Unknown outbox message type {Type}", message.Type);
                    break;
            }

            message.ProcessedOn = DateTime.UtcNow;
            message.Error = null;

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing outbox message {Id}", message.Id);
            message.Error = ex.Message;
            // ProcessedOn queda null para que se reintente en el siguiente ciclo.
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SendOrderConfirmationEmailAsync(
        ApplicationDbContext db,
        IEmailService emailService,
        IUserService userService,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<OrderCreatedPayload>(message.Content);

        if (payload == null)
        {
            throw new InvalidOperationException(
                "No fue posible leer el payload del evento OrderCreated.");
        }

        var order = await db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(o => o.Id == payload.Id, cancellationToken);

        if (order == null)
        {
            throw new InvalidOperationException(
                $"No se encontró el pedido {payload.Id} para enviar la confirmación.");
        }

        var users = await userService.GetUsersByIdsAsync(
            new[] { order.UserId }, cancellationToken);

        if (!users.TryGetValue(order.UserId, out var customer) ||
            string.IsNullOrWhiteSpace(customer.Email))
        {
            throw new InvalidOperationException(
                $"No se encontró el correo del cliente para el pedido {order.Id}.");
        }

        var (subject, html) = EmailTemplates.OrderConfirmation(order);

        await emailService.SendAsync(customer.Email, subject, html, cancellationToken);
    }

    private static async Task SendOrderShippedEmailAsync(
        ApplicationDbContext db,
        IEmailService emailService,
        IUserService userService,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<OrderShippedPayload>(message.Content);

        if (payload == null)
        {
            throw new InvalidOperationException(
                "No fue posible leer el payload del evento OrderShipped.");
        }

        var order = await db.Orders
            .FirstOrDefaultAsync(o => o.Id == payload.Id, cancellationToken);

        if (order == null)
        {
            throw new InvalidOperationException(
                $"No se encontró el pedido {payload.Id} para enviar el aviso de envío.");
        }

        var users = await userService.GetUsersByIdsAsync(
            new[] { order.UserId }, cancellationToken);

        if (!users.TryGetValue(order.UserId, out var customer) ||
            string.IsNullOrWhiteSpace(customer.Email))
        {
            throw new InvalidOperationException(
                $"No se encontró el correo del cliente para el pedido {order.Id}.");
        }

        var (subject, html) = EmailTemplates.OrderShipped(order);

        await emailService.SendAsync(customer.Email, subject, html, cancellationToken);
    }

    private record OrderCreatedPayload(Guid Id, string OrderNumber, decimal Total, Guid UserId);

    private record OrderShippedPayload(Guid Id, string OrderNumber, Guid UserId);
}
