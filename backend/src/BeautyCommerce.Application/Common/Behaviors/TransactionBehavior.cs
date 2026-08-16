using MediatR;
using Microsoft.Extensions.Logging;
using BeautyCommerce.Application.Common.Exceptions;
using BeautyCommerce.Application.Common.Interfaces;

namespace BeautyCommerce.Application.Common.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
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
            if (IsExpectedBusinessException(ex))
            {
                _logger.LogWarning(ex, "Transaction rolled back for {Request}: {Message}", typeof(TRequest).Name, ex.Message);
            }
            else
            {
                _logger.LogError(ex, "Transaction failed for {Request}", typeof(TRequest).Name);
            }

            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool IsExpectedBusinessException(Exception exception) =>
        exception is BadRequestException
            or NotFoundException
            or ConflictException
            or UnauthorizedException
            or UnauthorizedAccessException
            or FluentValidation.ValidationException;
}
