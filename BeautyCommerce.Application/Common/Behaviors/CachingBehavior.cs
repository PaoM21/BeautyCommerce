using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BeautyCommerce.Application.Common.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly BeautyCommerce.Application.Common.Interfaces.ICacheService? _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(BeautyCommerce.Application.Common.Interfaces.ICacheService? cache,
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_cache == null || !typeof(TRequest).Name.EndsWith("Query"))
            return await next();

        if (typeof(TRequest).Name.Contains("ShoppingCart"))
            return await next();

        var key = $"{typeof(TRequest).FullName}:{JsonSerializer.Serialize(request)}";

        var cached = await _cache.GetAsync<TResponse>(key);
        if (cached != null)
        {
            _logger.LogInformation("Cache hit for {Key}", key);
            return cached;
        }

        var response = await next();

        // Cache for default 5 minutes, tagged by feature so write handlers
        // can invalidate every cached query for that feature (list + detail)
        // without needing to know each individual cache key.
        await _cache.SetAsync(key, response!, TimeSpan.FromMinutes(5), GetFeatureTag(typeof(TRequest)));

        return response;
    }

    private static string? GetFeatureTag(Type requestType)
    {
        const string marker = ".Features.";

        var ns = requestType.Namespace;

        var markerIndex = ns?.IndexOf(marker, StringComparison.Ordinal) ?? -1;

        if (markerIndex < 0)
            return null;

        var rest = ns![(markerIndex + marker.Length)..];

        var dotIndex = rest.IndexOf('.');

        return dotIndex < 0 ? rest : rest[..dotIndex];
    }
}
