using Google.Apis.People.v1;
using Google.Apis.People.v1.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net.Http;

namespace console.Services;

public class GooglePeopleService : IGooglePeopleService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GooglePeopleService> _logger;
    private readonly IGoogleAuthService _authService;
    private PeopleService? _cachedService;
    private readonly SemaphoreSlim _serviceLock = new(1, 1);

    public GooglePeopleService(
        IConfiguration configuration,
        ILogger<GooglePeopleService> logger,
        IGoogleAuthService authService)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    private async Task<PeopleService> GetPeopleServiceAsync(CancellationToken cancellationToken = default)
    {
        // 使用雙重檢查鎖定模式來快取 service
        if (_cachedService != null)
        {
            return _cachedService;
        }

        await _serviceLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedService != null)
            {
                return _cachedService;
            }

            var credential = await _authService.GetCredentialAsync(cancellationToken);
            
            _cachedService = new PeopleService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "TWDIW Visitor System Console"
            });

            return _cachedService;
        }
        finally
        {
            _serviceLock.Release();
        }
    }

    public async Task<List<Contact>> GetContactsAsync()
    {
        const int maxRetries = 3;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                var service = await GetPeopleServiceAsync();
                
                _logger.LogInformation("Fetching contacts from Google People API using listDirectoryPeople");

                var allPeople = await GetContactsViaHttpAsync(service, null);

                _logger.LogInformation("Total contacts fetched: {Count}", allPeople.Count);

                var contacts = allPeople.Select(MapToContact).ToList();

                return contacts;
            }
            catch (Exception ex) when (retryCount < maxRetries - 1)
            {
                retryCount++;
                _logger.LogWarning(ex, 
                    "Error fetching contacts (attempt {Attempt}/{MaxRetries}), retrying...", 
                    retryCount, maxRetries);
                
                // 如果認證失敗，清除快取的 service
                if (ex.Message.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
                {
                    _cachedService = null;
                }
                
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contacts after {Attempt} attempts", retryCount + 1);
                throw;
            }
        }

        throw new InvalidOperationException("Failed to fetch contacts after maximum retries");
    }

    private static Contact MapToContact(Person person)
    {
        var displayName = person.Names?.FirstOrDefault()?.DisplayName ?? "無名稱";
        var primaryEmail = person.EmailAddresses?.FirstOrDefault()?.Value ?? string.Empty;
        var primaryPhone = person.PhoneNumbers?.FirstOrDefault()?.Value ?? string.Empty;

        string? company = null;
        string? department = null;
        string? title = null;
        
        if (person.Organizations?.Any() == true)
        {
            var org = person.Organizations.First();
            company = org.Name;
            department = org.Department;
            title = org.Title;
        }

        return new Contact(
            Id: person.ResourceName ?? string.Empty,
            Name: displayName,
            Email: primaryEmail,
            Phone: primaryPhone,
            Company: company,
            Department: department,
            Title: title
        );
    }

    private async Task<List<Person>> GetContactsViaHttpAsync(PeopleService service, string? initialPageToken)
    {
        var allPeople = new List<Person>();
        string? pageToken = initialPageToken;

        while (true)
        {
            // 建立請求 URL（與 Python 相同）
            // API 端點: GET https://people.googleapis.com/v1/people:listDirectoryPeople
            var url = "https://people.googleapis.com/v1/people:listDirectoryPeople";
            var queryParams = new List<string>
            {
                "readMask=names,emailAddresses,phoneNumbers,organizations",
                "sources=DIRECTORY_SOURCE_TYPE_DOMAIN_PROFILE"
            };

            if (!string.IsNullOrEmpty(pageToken))
            {
                queryParams.Add($"pageToken={Uri.EscapeDataString(pageToken)}");
            }

            url += "?" + string.Join("&", queryParams);

            _logger.LogInformation("Fetching contacts via HTTP... (already fetched {Count})", allPeople.Count);

            // 使用 service 的 HttpClient，它已經配置了認證
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await service.HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);

            var people = new List<Person>();
            if (result.TryGetProperty("people", out var peopleProp))
            {
                foreach (var personJson in peopleProp.EnumerateArray())
                {
                    // 將 JSON 轉換為 Person 物件
                    var personJsonString = personJson.GetRawText();
                    var person = JsonSerializer.Deserialize<Person>(personJsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (person != null)
                    {
                        people.Add(person);
                    }
                }
            }

            if (people.Any())
            {
                allPeople.AddRange(people);
                _logger.LogInformation("Fetched {Count} contacts in this batch", people.Count);
            }

            if (result.TryGetProperty("nextPageToken", out var nextPageTokenProp))
            {
                pageToken = nextPageTokenProp.GetString();
                if (string.IsNullOrEmpty(pageToken))
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        return allPeople;
    }

    public async Task SyncContactsToDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("Fetching contacts from Google People API...");
            var contacts = await GetContactsAsync();
            
            if (contacts.Count == 0)
            {
                _logger.LogInformation("No contacts found to sync");
                return;
            }

            _logger.LogInformation("Found {Count} contacts to sync", contacts.Count);
            
            // 記錄所有聯絡人的詳細資訊
            foreach (var contact in contacts)
            {
                _logger.LogInformation("  - Contact: {Name} | Email: {Email} | Phone: {Phone} | Company: {Company} | Title: {Title} | Department: {Department}", 
                    contact.Name, 
                    contact.Email ?? "N/A", 
                    contact.Phone ?? "N/A", 
                    contact.Company ?? "N/A",
                    contact.Title ?? "N/A",
                    contact.Department ?? "N/A");
            }

            // TODO: 實作寫入資料庫邏輯
            // 1. 檢查聯絡人是否已存在
            // 2. 新增或更新聯絡人資料
            // 3. 處理組織架構資訊

            await Task.CompletedTask;
            
            _logger.LogInformation("Successfully processed {Count} contacts (database sync pending)", contacts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing contacts to database: {Message}", ex.Message);
            throw;
        }
    }
}
