using StackExchange.Redis;
using System.Text.Json;

namespace web.Services;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _database = redis.GetDatabase();
    }

    public async Task<string?> GetAsync(string key)
    {
        var value = await _database.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task SetAsync(string key, string value, TimeSpan? expiration = null)
    {
        if (expiration.HasValue)
        {
            await _database.StringSetAsync(key, value, expiration);
        }
        else
        {
            await _database.StringSetAsync(key, value);
        }
    }

    public async Task DeleteAsync(string key)
    {
        await _database.KeyDeleteAsync(key);
    }

    public async Task<IEnumerable<string>> GetKeysAsync(string pattern)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = new List<string>();
        await foreach (var key in server.KeysAsync(pattern: pattern))
        {
            keys.Add(key.ToString());
        }
        return keys;
    }

    public async Task<bool> TryAcquireLockAsync(string key, string value, TimeSpan expiration)
    {
        // 使用 SET NX EX 命令實現分布式鎖
        // NX: 只在鍵不存在時設置
        // EX: 設置過期時間（秒）
        return await _database.StringSetAsync(key, value, expiration, When.NotExists);
    }

    public async Task<bool> ReleaseLockAsync(string key, string value)
    {
        // 使用 Lua 腳本確保原子性：只有當值匹配時才刪除
        const string script = @"
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end
        ";
        
        var result = await _database.ScriptEvaluateAsync(
            script,
            new RedisKey[] { key },
            new RedisValue[] { value }
        );
        
        return result.Type == ResultType.Integer && (int)result == 1;
    }
}

