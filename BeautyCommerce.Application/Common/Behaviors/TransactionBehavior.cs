using MediatR;
using Microsoft.Extensions.Logging;
using BeautyCommerce.Application.Common.Interfaces;

namespace BeautyCommerce.Application.Common.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(IApplicationDbContext context, ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only wrap commands to avoid unnecessary transactions for queries
        if (!typeof(TRequest).Name.EndsWith("Command"))
            return await next();

        var response = default(TResponse)!;

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            response = await next();

            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction failed for {Request}", typeof(TRequest).Name);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
