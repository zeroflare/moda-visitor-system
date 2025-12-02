using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace console.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleAuthService> _logger;
    private readonly string _clientSecretPath;
    private readonly string _tokenPath;
    private ClientSecrets? _clientSecrets;

    public GoogleAuthService(
        IConfiguration configuration,
        ILogger<GoogleAuthService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _clientSecretPath = _configuration["Google:ClientSecretPath"] ?? "client_secret.json";
        _tokenPath = _configuration["Google:TokenPath"] ?? "token.json";
    }

    public async Task<UserCredential> GetCredentialAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_tokenPath))
        {
            throw new FileNotFoundException(
                $"Token file not found: {_tokenPath}. Please run authentication setup first.");
        }

        var tokenJson = await File.ReadAllTextAsync(_tokenPath);
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);
        
        _clientSecrets = await LoadClientSecretsAsync(cancellationToken);
        
        // 處理 Python 生成的 token.json 格式
        var accessToken = GetTokenProperty(tokenData, "token", "access_token");
        var refreshToken = GetTokenProperty(tokenData, "refresh_token");
        var expiry = ParseExpiry(tokenData);

        var tokenResponse = new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresInSeconds = expiry.HasValue && expiry.Value > DateTime.UtcNow
                ? (long?)(expiry.Value - DateTime.UtcNow).TotalSeconds 
                : null
        };
        
        var flow = new GoogleAuthorizationCodeFlow(
            new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = _clientSecrets
            });
        
        var credential = new UserCredential(flow, "user", tokenResponse);

        // 如果 token 過期，自動刷新
        if (credential.Token.IsExpired(SystemClock.Default) && !string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogInformation("Access token expired, refreshing...");
            await credential.RefreshTokenAsync(cancellationToken);
            await SaveTokenAsync(credential, cancellationToken);
            _logger.LogInformation("Access token refreshed successfully");
        }

        return credential;
    }

    public async Task SaveTokenAsync(UserCredential credential, CancellationToken cancellationToken = default)
    {
        JsonElement originalTokenData;
        
        // 讀取現有的 token.json 以保留原有資料
        if (File.Exists(_tokenPath))
        {
            var tokenJson = await File.ReadAllTextAsync(_tokenPath);
            originalTokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);
        }
        else
        {
            originalTokenData = JsonSerializer.Deserialize<JsonElement>("{}");
        }

        var refreshToken = credential.Token.RefreshToken 
            ?? GetTokenProperty(originalTokenData, "refresh_token");
        
        var clientId = _clientSecrets?.ClientId 
            ?? GetTokenProperty(originalTokenData, "client_id");
        
        var clientSecret = _clientSecrets?.ClientSecret 
            ?? GetTokenProperty(originalTokenData, "client_secret");
        
        var tokenData = new Dictionary<string, object?>
        {
            ["token"] = credential.Token.AccessToken,
            ["refresh_token"] = refreshToken,
            ["token_uri"] = "https://oauth2.googleapis.com/token",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
        };
        
        // 保留原有的 scopes
        if (originalTokenData.TryGetProperty("scopes", out var scopes))
        {
            tokenData["scopes"] = scopes;
        }
        
        // 計算過期時間
        if (credential.Token.IssuedUtc != default && credential.Token.ExpiresInSeconds.HasValue)
        {
            var expiry = credential.Token.IssuedUtc.AddSeconds(credential.Token.ExpiresInSeconds.Value);
            tokenData["expiry"] = expiry.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }
        
        await File.WriteAllTextAsync(_tokenPath, JsonSerializer.Serialize(tokenData, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        }), cancellationToken);
    }

    private async Task<ClientSecrets> LoadClientSecretsAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_clientSecretPath))
        {
            throw new FileNotFoundException(
                $"Client secret file not found: {_clientSecretPath}");
        }

        var clientSecretsJson = await File.ReadAllTextAsync(_clientSecretPath);
        var clientSecretsData = JsonSerializer.Deserialize<JsonElement>(clientSecretsJson);
        
        if (!clientSecretsData.TryGetProperty("installed", out var installed))
        {
            throw new InvalidOperationException(
                "Invalid client_secret.json format: 'installed' property not found");
        }
        
        var clientId = installed.GetProperty("client_id").GetString();
        var clientSecret = installed.GetProperty("client_secret").GetString();
        
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException(
                "Invalid client_secret.json: client_id or client_secret is missing");
        }
        
        return new ClientSecrets
        {
            ClientId = clientId,
            ClientSecret = clientSecret
        };
    }

    private static string? GetTokenProperty(JsonElement tokenData, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (tokenData.TryGetProperty(propertyName, out var prop))
            {
                return prop.GetString();
            }
        }
        return null;
    }

    private static DateTime? ParseExpiry(JsonElement tokenData)
    {
        if (!tokenData.TryGetProperty("expiry", out var expiryProp))
        {
            return null;
        }

        var expiryStr = expiryProp.GetString();
        if (string.IsNullOrEmpty(expiryStr))
        {
            return null;
        }

        return DateTime.TryParse(expiryStr, out var expiryDate) ? expiryDate : null;
    }
}

