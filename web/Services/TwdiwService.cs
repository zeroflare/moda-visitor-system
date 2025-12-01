using System.Text;
using System.Text.Json;
using web.Models;

namespace web.Services;

public class TwdiwService : ITwdiwService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TwdiwService> _logger;

    public TwdiwService(HttpClient httpClient, IConfiguration configuration, ILogger<TwdiwService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
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
            MeetingRoom: "延平 201 會議室"
        );
    }

    public async Task<SubmitRegistrationResponse> SubmitRegistrationAsync(SubmitRegistrationRequest request)
    {
        var apiUrl = _configuration["Twdiw:VcUrl"];
        var token = _configuration["Twdiw:VcToken"];
        var vcId = _configuration["Twdiw:VcId"];

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
            }
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

        return new SubmitRegistrationResponse(
            Message: "Registration successful",
            TransactionId: data.GetProperty("transactionId").GetString() ?? string.Empty,
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
            return new { Message = "Registration completed", Data = data };
        }
        else
        {
            return new { Message = "Waiting for registration" };
        }
    }
}

