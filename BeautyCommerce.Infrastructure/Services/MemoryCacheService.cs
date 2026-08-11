using System.Collections.Concurrent;
using BeautyCommerce.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace BeautyCommerce.Infrastructure.Services;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    // Shared across requests: ICacheService is registered as Scoped, but tag
    // tokens must outlive a single request to invalidate entries cached by
    // earlier requests, so this is static rather than an instance field.
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _tagTokens = new();

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        if (_cache.TryGetValue(key, out T value))
            return Task.FromResult<T?>(value);

        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration, string? tag = null)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration,
        };

        if (tag != null)
        {
            var tokenSource = _tagTokens.GetOrAdd(tag, _ => new CancellationTokenSource());

            options.AddExpirationToken(new CancellationChangeToken(tokenSource.Token));
        }

        _cache.Set(key, value, options);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);

        return Task.CompletedTask;
    }

    public Task InvalidateTagAsync(string tag)
    {
        if (_tagTokens.TryRemove(tag, out var tokenSource))
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
        }

        return Task.CompletedTask;
    }
}
