using Microsoft.Extensions.Caching.Memory;

namespace ERPAccounting.Infrastructure.Services;

/// <summary>
/// Thin wrapper around <see cref="IMemoryCache"/> that exposes async helpers.
/// Keeps infrastructure level caching logic centralized.
/// </summary>
public class CacheService
{
    private readonly IMemoryCache _memoryCache;

    public CacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public async Task<T> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? lifetime = null)
    {
        if (_memoryCache.TryGetValue(cacheKey, out T? cached) && cached is not null)
        {
            return cached;
        }

        var value = await factory();
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = lifetime ?? TimeSpan.FromMinutes(30)
        };

        _memoryCache.Set(cacheKey, value, options);
        return value;
    }

    public void Remove(string cacheKey) => _memoryCache.Remove(cacheKey);
}
