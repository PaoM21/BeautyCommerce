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

        // These features change on every write from the same request that
        // reads them (cart/wishlist contents, loyalty points, admin
        // analytics fed by many unrelated writers), and none of their write
        // handlers invalidate a cache tag, so caching them only ever serves
        // stale data. Excluding by feature tag (rather than matching
        // substrings against the request class name) is what actually stays
        // correct regardless of how an individual query class is named — a
        // prior version checked Name.Contains("ShoppingCart"), which
        // silently never matched "GetCartQuery" and left the cart cached
        // for 5 minutes after every add/update/remove; the same gap existed
        // for Wishlist (remove-then-re-add of the same product also 500'd
        // on a stale unique-constraint row — see AddWishlistCommandHandler).
        if (tag is "ShoppingCart" or "Dashboard" or "Wishlist" or "Loyalty")
            return await next();

        // Many per-user queries (GetMyOrdersQuery, ...) take no parameters
        // at all — they resolve "the current user" from ICurrentUserService
        // inside the handler. Without partitioning the cache key by user,
        // every such query collapses onto the exact same key, so the first
        // user to populate the cache leaks their private data to every
        // other authenticated user who hits that endpoint within the cache
        // window. Always scoping the key to the current user (when there is
        // one) closes this for every existing and future per-user query,
        // instead of relying on someone remembering to add each new one to
        // an exclusion list.
        var userScope = _currentUser.UserId?.ToString() ?? "anonymous";

        var key = $"{typeof(TRequest).FullName}:{userScope}:{JsonSerializer.Serialize(request)}";

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
