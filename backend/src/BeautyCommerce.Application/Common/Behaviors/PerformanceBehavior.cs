using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BeautyCommerce.Application.Common.Behaviors;

public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly int _thresholdMilliseconds = 500;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > _thresholdMilliseconds)
        {
            _logger.LogWarning("{Request} took {Elapsed} ms", typeof(TRequest).Name, stopwatch.ElapsedMilliseconds);
        }

        return response;
    }
}
