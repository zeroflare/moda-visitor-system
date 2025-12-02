namespace web.Services;

public interface ICacheService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value, TimeSpan? expiration = null);
    Task DeleteAsync(string key);
    Task<IEnumerable<string>> GetKeysAsync(string pattern);
    
    /// <summary>
    /// 嘗試獲取分布式鎖
    /// </summary>
    /// <param name="key">鎖的鍵</param>
    /// <param name="value">鎖的值（通常使用唯一標識符）</param>
    /// <param name="expiration">鎖的過期時間</param>
    /// <returns>如果成功獲取鎖返回 true，否則返回 false</returns>
    Task<bool> TryAcquireLockAsync(string key, string value, TimeSpan expiration);
    
    /// <summary>
    /// 釋放分布式鎖
    /// </summary>
    /// <param name="key">鎖的鍵</param>
    /// <param name="value">鎖的值（必須與獲取時的值相同）</param>
    /// <returns>如果成功釋放鎖返回 true，否則返回 false</returns>
    Task<bool> ReleaseLockAsync(string key, string value);
}

