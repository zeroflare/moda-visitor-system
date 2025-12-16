using System.Collections.Concurrent;

namespace web.Services;

/// <summary>
/// 簡單的內存緩存服務實現，用於 Redis 不可用時的 fallback
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, (string Value, DateTime? ExpiresAt)> _cache = new();
    private readonly ConcurrentDictionary<string, (string Value, DateTime ExpiresAt)> _locks = new();

    public Task<string?> GetAsync(string key)
    {
        if (_cache.TryGetValue(key, out var item))
        {
            if (item.ExpiresAt == null || item.ExpiresAt > DateTime.UtcNow)
            {
                return Task.FromResult<string?>(item.Value);
            }
            else
            {
                // 過期，移除
                _cache.TryRemove(key, out _);
            }
        }
        return Task.FromResult<string?>(null);
    }

    public Task SetAsync(string key, string value, TimeSpan? expiration = null)
    {
        var expiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : (DateTime?)null;
        _cache.AddOrUpdate(key, (value, expiresAt), (k, v) => (value, expiresAt));
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key)
    {
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetKeysAsync(string pattern)
    {
        // 簡單的實現，只支持完全匹配
        var keys = _cache.Keys.Where(k => k == pattern || k.Contains(pattern.Replace("*", "")));
        return Task.FromResult(keys);
    }

    public Task<bool> TryAcquireLockAsync(string key, string value, TimeSpan expiration)
    {
        var expiresAt = DateTime.UtcNow.Add(expiration);
        
        // 嘗試添加鎖，如果已存在且未過期則返回 false
        if (_locks.TryGetValue(key, out var existingLock))
        {
            if (existingLock.ExpiresAt > DateTime.UtcNow)
            {
                return Task.FromResult(false);
            }
        }
        
        _locks.AddOrUpdate(key, (value, expiresAt), (k, v) => (value, expiresAt));
        return Task.FromResult(true);
    }

    public Task<bool> ReleaseLockAsync(string key, string value)
    {
        if (_locks.TryGetValue(key, out var existingLock))
        {
            if (existingLock.Value == value)
            {
                _locks.TryRemove(key, out _);
                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }
}

