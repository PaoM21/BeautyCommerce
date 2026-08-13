using BeautyCommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BeautyCommerce.Application.Common.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICacheService? _cache;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(
        ICacheService? cache,
        ICurrentUserService currentUser,
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_cache == null || !typeof(TRequest).Name.EndsWith("Query"))
            return await next();

        var tag = GetFeatureTag(typeof(TRequest));

        if (tag is "ShoppingCart" or "Dashboard" or "Wishlist" or "Loyalty")
            return await next();

        var userScope = _currentUser.UserId?.ToString() ?? "anonymous";

        var key = $"{typeof(TRequest).FullName}:{userScope}:{JsonSerializer.Serialize(request)}";

        var cached = await _cache.GetAsync<TResponse>(key);
        if (cached != null)
        {
            _logger.LogInformation("Cache hit for {Key}", key);
            return cached;
        }

        var response = await next();
        
        await _cache.SetAsync(key, response!, TimeSpan.FromMinutes(5), tag);

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
