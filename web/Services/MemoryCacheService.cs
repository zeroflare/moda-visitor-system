using Microsoft.Extensions.Caching.Memory;

namespace web.Services;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<string?> GetAsync(string key)
    {
        _cache.TryGetValue(key, out string? value);
        return Task.FromResult(value);
    }

    public Task SetAsync(string key, string value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration;
        }
        _cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
}

