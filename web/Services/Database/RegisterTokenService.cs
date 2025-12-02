using System.Text.Json;
using web.Models;

namespace web.Services;

public class RegisterTokenService : IRegisterTokenService
{
    private readonly ICacheService _cacheService;
    private const string TokenKeyPrefix = "register_token:";
    private const string TokenIndexKey = "register_tokens:index";

    public RegisterTokenService(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<IEnumerable<RegisterToken>> GetAllRegisterTokensAsync()
    {
        var keys = await _cacheService.GetKeysAsync($"{TokenKeyPrefix}*");
        var tokens = new List<RegisterToken>();

        foreach (var key in keys)
        {
            var json = await _cacheService.GetAsync(key);
            if (!string.IsNullOrEmpty(json))
            {
                var token = JsonSerializer.Deserialize<RegisterToken>(json);
                if (token != null)
                {
                    tokens.Add(token);
                }
            }
        }

        return tokens.OrderByDescending(t => t.CreatedAt);
    }

    public async Task<RegisterToken?> GetRegisterTokenByIdAsync(string id)
    {
        var key = $"{TokenKeyPrefix}{id}";
        var json = await _cacheService.GetAsync(key);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<RegisterToken>(json);
    }

    public async Task<IEnumerable<RegisterToken>> GetRegisterTokensByEmailAsync(string visitorEmail)
    {
        var allTokens = await GetAllRegisterTokensAsync();
        return allTokens.Where(t => t.VisitorEmail == visitorEmail);
    }

    public async Task<RegisterToken> CreateRegisterTokenAsync(RegisterToken registerToken)
    {
        var key = $"{TokenKeyPrefix}{registerToken.Id}";
        var json = JsonSerializer.Serialize(registerToken);

        // 計算過期時間（使用 expired_at 或預設 24 小時）
        var expiration = registerToken.ExpiredAt > DateTime.UtcNow
            ? registerToken.ExpiredAt - DateTime.UtcNow
            : TimeSpan.FromHours(24);

        await _cacheService.SetAsync(key, json, expiration);
        return registerToken;
    }

    public async Task<RegisterToken?> UpdateRegisterTokenAsync(string id, RegisterToken registerToken)
    {
        var existingToken = await GetRegisterTokenByIdAsync(id);
        if (existingToken == null)
        {
            return null;
        }

        var key = $"{TokenKeyPrefix}{id}";
        var json = JsonSerializer.Serialize(registerToken);

        // 計算過期時間
        var expiration = registerToken.ExpiredAt > DateTime.UtcNow
            ? registerToken.ExpiredAt - DateTime.UtcNow
            : TimeSpan.FromHours(24);

        await _cacheService.SetAsync(key, json, expiration);
        return registerToken;
    }

    public async Task<bool> DeleteRegisterTokenAsync(string id)
    {
        var key = $"{TokenKeyPrefix}{id}";
        var existingToken = await GetRegisterTokenByIdAsync(id);
        if (existingToken == null)
        {
            return false;
        }

        await _cacheService.DeleteAsync(key);
        return true;
    }
}
