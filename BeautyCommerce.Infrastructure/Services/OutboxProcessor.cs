using System.Text.Json;
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

                var messages = await db.OutboxMessages
                    .Where(m => m.ProcessedOn == null)
                    .OrderBy(m => m.OccurredOn)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var msg in messages)
                {
                    try
                    {
                        _logger.LogInformation("Processing outbox message {Id} Type={Type}", msg.Id, msg.Type);

                        // Very small dispatcher - extend with DI based handlers as needed
                        switch (msg.Type)
                        {
                            case "OrderCreated":
                                // For demo: log payload
                                _logger.LogInformation("OrderCreated payload: {Payload}", msg.Content);
                                break;

                            default:
                                _logger.LogWarning("Unknown outbox message type {Type}", msg.Type);
                                break;
                        }

                        msg.ProcessedOn = DateTime.UtcNow;
                        msg.Error = null;

                        await db.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing outbox message {Id}", msg.Id);
                        msg.Error = ex.Message;
                        // keep ProcessedOn null so it can be retried
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in outbox processing loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
