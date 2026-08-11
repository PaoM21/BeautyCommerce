namespace BeautyCommerce.Application.Common.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan expiration,
        string? tag = null);

    Task RemoveAsync(string key);

    Task InvalidateTagAsync(string tag);
}
