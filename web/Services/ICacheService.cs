namespace web.Services;

public interface ICacheService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value, TimeSpan? expiration = null);
    Task DeleteAsync(string key);
    Task<IEnumerable<string>> GetKeysAsync(string pattern);
}

