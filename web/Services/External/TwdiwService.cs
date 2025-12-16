using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using web.Models;

namespace web.Services;

public class TwdiwService : ITwdiwService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TwdiwService> _logger;
    private readonly ICacheService _cacheService;
    private readonly IVisitorProfileService _visitorProfileService;

    public TwdiwService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<TwdiwService> logger,
        ICacheService cacheService,
        IVisitorProfileService visitorProfileService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _visitorProfileService = visitorProfileService ?? throw new ArgumentNullException(nameof(visitorProfileService));
    }

    public async Task<QRCodeResponse> GetQRCodeAsync(string transactionId, string? counter = null)
    {
        var apiUrl = _configuration["Twdiw:VpUrl"];
        var token = _configuration["Twdiw:VpToken"];
        var refId = _configuration["Twdiw:VpIdCheckin"];

        var url = $"{apiUrl}/api/oidvp/qrcode?ref={refId}&transactionId={transactionId}";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Access-Token", token);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<QRCodeResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return data ?? new QRCodeResponse(string.Empty, string.Empty);
    }

    public async Task<CheckinResultResponse> GetCheckinResultAsync(string transactionId)
    {
        var apiUrl = _configuration["Twdiw:VpUrl"];
        var token = _configuration["Twdiw:VpToken"];

        var url = $"{apiUrl}/api/oidvp/result";
        
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Access-Token", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { transactionId }),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Checkin result error: {Error}", text);
            throw new HttpRequestException("等待驗證");
        }

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(json);

        var claims = data.GetProperty("data")[0].GetProperty("claims").EnumerateArray();
        var email = claims.FirstOrDefault(c => c.GetProperty("ename").GetString() == "email")
            .GetProperty("value").GetString();
        var name = claims.FirstOrDefault(c => c.GetProperty("ename").GetString() == "name")
            .GetProperty("value").GetString();
        var company = claims.FirstOrDefault(c => c.GetProperty("ename").GetString() == "company")
            .GetProperty("value").GetString();
        var phone = claims.FirstOrDefault(c => c.GetProperty("ename").GetString() == "phone")
            .GetProperty("value").GetString();

        return new CheckinResultResponse(
            InviterEmail: "a@moda.gov.tw",
            InviterName: "邀請者姓名",
            InviterDept: "邀請者單位",
            InviterTitle: "邀請者職稱",
            VisitorEmail: email,
            VisitorName: name,
            VisitorDept: company,
            VisitorPhone: phone,
            MeetingTime: "2025-11-27 10:00:00",
            MeetingName: "",
            MeetingRoom: "延平 201 會議室"
        );
    }

    public async Task<SubmitRegistrationResponse> SubmitRegistrationAsync(SubmitRegistrationRequest request)
    {
        var apiUrl = _configuration["Twdiw:VcUrl"];
        var token = _configuration["Twdiw:VcToken"];
        var vcId = _configuration["Twdiw:VcId"];

        // 從資料庫查詢相同 email 的 VisitorProfile，收集所有 cid
        var cids = new List<string>();
        try
        {
            var existingProfile = await _visitorProfileService.GetVisitorProfileByEmailAsync(request.Email);
            if (existingProfile != null && !string.IsNullOrWhiteSpace(existingProfile.Cid))
            {
                cids.Add(existingProfile.Cid);
                _logger.LogInformation("Found existing CID for email {Email}: {Cid}", request.Email, existingProfile.Cid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error querying VisitorProfile for email {Email}, continuing without cids", request.Email);
        }

        var url = $"{apiUrl}/api/qrcode/data";
        
        var payload = new
        {
            vcUid = vcId,
            fields = new[]
            {
                new { ename = "name", content = request.Name },
                new { ename = "email", content = request.Email },
                new { ename = "company", content = request.Company },
                new { ename = "phone", content = request.Phone }
            },
            cids = cids.ToArray()
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add("Access-Token", token);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(json);

        var transactionId = data.GetProperty("transactionId").GetString() ?? string.Empty;

        // 將註冊資料儲存到 Redis，使用 TransactionId 作為 key
        if (!string.IsNullOrEmpty(transactionId))
        {
            var registrationData = new
            {
                name = request.Name,
                email = request.Email,
                company = request.Company,
                phone = request.Phone
            };

            var registrationDataJson = JsonSerializer.Serialize(registrationData);
            var redisKey = $"registration:{transactionId}";
            
            // 儲存 24 小時（註冊流程應該在 24 小時內完成）
            await _cacheService.SetAsync(redisKey, registrationDataJson, TimeSpan.FromHours(24));
            _logger.LogInformation("Registration data stored in Redis for transactionId: {TransactionId}", transactionId);
        }

        return new SubmitRegistrationResponse(
            Message: "Registration successful",
            TransactionId: transactionId,
            QrcodeImage: data.GetProperty("qrCode").GetString() ?? string.Empty,
            AuthUri: data.GetProperty("deepLink").GetString() ?? string.Empty
        );
    }

    public async Task<object> GetRegistrationResultAsync(string transactionId)
    {
        var apiUrl = _configuration["Twdiw:VcUrl"];
        var token = _configuration["Twdiw:VcToken"];

        var url = $"{apiUrl}/api/credential/nonce/{transactionId}";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Access-Token", token);

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            
            // 從 Redis 讀取註冊資料
            var redisKey = $"registration:{transactionId}";
            var registrationDataJson = await _cacheService.GetAsync(redisKey);
            
            if (!string.IsNullOrEmpty(registrationDataJson))
            {
                try
                {
                    var registrationData = JsonSerializer.Deserialize<JsonElement>(registrationDataJson);
                    var email = registrationData.GetProperty("email").GetString();
                    var name = registrationData.GetProperty("name").GetString();
                    var company = registrationData.GetProperty("company").GetString();
                    var phone = registrationData.GetProperty("phone").GetString();

                    // 從 response 中取得 credential (JWT Token)
                    string? cid = null;
                    DateTime? expiresAt = null;
                    if (data.TryGetProperty("credential", out var credentialElement))
                    {
                        var credential = credentialElement.GetString();
                        if (!string.IsNullOrEmpty(credential))
                        {
                            var (extractedCid, extractedExpiresAt) = ExtractCidAndExpirationFromJwt(credential);
                            cid = extractedCid;
                            expiresAt = extractedExpiresAt;
                        }
                    }

                    // 寫入 VisitorProfile 資料表
                    if (!string.IsNullOrEmpty(email))
                    {
                        var visitorProfile = new VisitorProfile
                        {
                            Email = email,
                            Name = name,
                            Company = company,
                            Phone = phone,
                            Cid = cid,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            ExpiresAt = expiresAt
                        };

                        await _visitorProfileService.CreateOrUpdateVisitorProfileAsync(visitorProfile);
                        _logger.LogInformation("Visitor profile created/updated for email: {Email}, CID: {Cid}, ExpiresAt: {ExpiresAt}", email, cid, expiresAt);

                        // 從 Redis 刪除已處理的資料
                        await _cacheService.DeleteAsync(redisKey);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing registration result for transactionId: {TransactionId}", transactionId);
                }
            }
            else
            {
                _logger.LogWarning("Registration data not found in Redis for transactionId: {TransactionId}", transactionId);
            }

            return new { Message = "Registration completed", Data = data };
        }
        else
        {
            return new { Message = "Waiting for registration" };
        }
    }

    private (string? cid, DateTime? expiresAt) ExtractCidAndExpirationFromJwt(string jwtToken)
    {
        try
        {
            // 解析 JWT Token（不驗證簽名，只讀取 claims）
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(jwtToken);
            
            string? cid = null;
            DateTime? expiresAt = null;
            
            // 取得 jti claim 來提取 CID
            if (jsonToken.Claims.FirstOrDefault(c => c.Type == "jti") is { } jtiClaim)
            {
                var jti = jtiClaim.Value;
                
                // jti 格式： "https://..../api/credential/a16187e9-755e-48ca-a9c0-622f76fe1360"
                // 提取 credential/ 後面的值
                var credentialPrefix = "/api/credential/";
                var credentialIndex = jti.IndexOf(credentialPrefix, StringComparison.OrdinalIgnoreCase);
                
                if (credentialIndex >= 0)
                {
                    cid = jti.Substring(credentialIndex + credentialPrefix.Length);
                    _logger.LogInformation("Extracted CID from JWT: {Cid}", cid);
                }
                else
                {
                    _logger.LogWarning("JTI does not contain credential prefix: {Jti}", jti);
                }
            }
            else
            {
                _logger.LogWarning("JTI claim not found in JWT token");
            }
            
            // 取得 exp claim 來提取過期時間
            if (jsonToken.Claims.FirstOrDefault(c => c.Type == "exp") is { } expClaim)
            {
                // exp 是 Unix timestamp (seconds since epoch)
                if (long.TryParse(expClaim.Value, out var expTimestamp))
                {
                    expiresAt = DateTimeOffset.FromUnixTimeSeconds(expTimestamp).UtcDateTime;
                    _logger.LogInformation("Extracted expiration from JWT: {ExpiresAt}", expiresAt);
                }
                else
                {
                    _logger.LogWarning("Invalid exp claim value: {ExpValue}", expClaim.Value);
                }
            }
            else
            {
                _logger.LogWarning("Exp claim not found in JWT token");
            }
            
            return (cid, expiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting CID and expiration from JWT token");
        }
        
        return (null, null);
    }
}

