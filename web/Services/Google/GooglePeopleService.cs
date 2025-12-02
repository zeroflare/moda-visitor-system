using Google.Apis.People.v1;
using Google.Apis.People.v1.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net.Http;
using web.Models;

namespace web.Services;

public class GooglePeopleService : IGooglePeopleService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GooglePeopleService> _logger;
    private readonly IGoogleAuthService _authService;
    private readonly IEmployeeService _employeeService;
    private PeopleService? _cachedService;
    private readonly SemaphoreSlim _serviceLock = new(1, 1);

    public GooglePeopleService(
        IConfiguration configuration,
        ILogger<GooglePeopleService> logger,
        IGoogleAuthService authService,
        IEmployeeService employeeService)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
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
                ApplicationName = "TWDIW Visitor System"
            });

            return _cachedService;
        }
        finally
        {
            _serviceLock.Release();
        }
    }

    public async Task SyncContactsToDatabaseAsync(CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                _logger.LogInformation("Fetching contacts from Google People API...");
                var contacts = await GetContactsAsync(cancellationToken);
                
                if (contacts.Count == 0)
                {
                    _logger.LogInformation("No contacts found to sync");
                    return;
                }

                _logger.LogInformation("Found {Count} contacts to sync", contacts.Count);
                
                // 轉換為 Employee 並同步到資料庫
                var employees = contacts
                    .Where(c => !string.IsNullOrWhiteSpace(c.Email) && !string.IsNullOrWhiteSpace(c.Id))
                    .Select(c => new Employee
                    {
                        Id = c.Id, // 直接使用 Google People ResourceName 作為 ID
                        Email = c.Email!,
                        Name = c.Name ?? "無名稱",
                        Dept = c.Department,
                        Costcenter = c.CostCenter,
                        Title = c.Title
                    })
                    .ToList();

                var syncedCount = await _employeeService.SyncEmployeesAsync(employees);
                
                _logger.LogInformation("Successfully synced {Count} employees to database", syncedCount);
                return;
            }
            catch (Exception ex) when (retryCount < maxRetries - 1)
            {
                retryCount++;
                _logger.LogWarning(ex, 
                    "Error syncing contacts (attempt {Attempt}/{MaxRetries}), retrying...", 
                    retryCount, maxRetries);
                
                // 如果認證失敗，清除快取的 service
                if (ex.Message.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
                {
                    _cachedService = null;
                }
                
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing contacts to database after {Attempt} attempts", retryCount + 1);
                throw;
            }
        }

        throw new InvalidOperationException("Failed to sync contacts after maximum retries");
    }

    private async Task<List<Contact>> GetContactsAsync(CancellationToken cancellationToken = default)
    {
        var service = await GetPeopleServiceAsync(cancellationToken);
        
        _logger.LogInformation("Fetching contacts from Google People API using listDirectoryPeople");

        var (people, peopleJson) = await GetContactsViaHttpAsync(service, null, cancellationToken);

        _logger.LogInformation("Total contacts fetched: {Count}", people.Count);

        var contacts = people.Zip(peopleJson, (person, json) => MapToContact(person, json)).ToList();

        return contacts;
    }

    private static Contact MapToContact(Person person, JsonElement personJson)
    {
        var displayName = person.Names?.FirstOrDefault()?.DisplayName ?? "無名稱";
        var primaryEmail = person.EmailAddresses?.FirstOrDefault()?.Value ?? string.Empty;
        var primaryPhone = person.PhoneNumbers?.FirstOrDefault()?.Value ?? string.Empty;

        string? company = null;
        string? department = null;
        string? title = null;
        string? costCenter = null;
        
        if (person.Organizations?.Any() == true)
        {
            var org = person.Organizations.First();
            company = org.Name;
            department = org.Department;
            title = org.Title;
            
            // 從原始 JSON 中提取 costCenter
            // 因為 Google.Apis.People.v1.Data.Organization 可能沒有 CostCenter 屬性
            if (personJson.TryGetProperty("organizations", out var orgsProp))
            {
                var orgsArray = orgsProp.EnumerateArray().ToList();
                if (orgsArray.Any())
                {
                    var firstOrgJson = orgsArray.First();
                    if (firstOrgJson.TryGetProperty("costCenter", out var costCenterProp))
                    {
                        costCenter = costCenterProp.GetString();
                    }
                }
            }
        }

        return new Contact(
            Id: person.ResourceName ?? string.Empty,
            Name: displayName,
            Email: primaryEmail,
            Phone: primaryPhone,
            Company: company,
            Department: department,
            Title: title,
            CostCenter: costCenter
        );
    }

    private async Task<(List<Person> People, List<JsonElement> PeopleJson)> GetContactsViaHttpAsync(PeopleService service, string? initialPageToken, CancellationToken cancellationToken)
    {
        var allPeople = new List<Person>();
        var allPeopleJson = new List<JsonElement>();
        string? pageToken = initialPageToken;

        while (true)
        {
            // 建立請求 URL
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
            var response = await service.HttpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<JsonElement>(json);

            var people = new List<Person>();
            var peopleJson = new List<JsonElement>();
            if (result.TryGetProperty("people", out var peopleProp))
            {
                foreach (var personJson in peopleProp.EnumerateArray())
                {
                    // 打印每個使用者的原始 JSON
                    var personJsonString = personJson.GetRawText();
                    var formattedJson = JsonSerializer.Serialize(
                        JsonSerializer.Deserialize<JsonElement>(personJsonString),
                        new JsonSerializerOptions { WriteIndented = true }
                    );
                    _logger.LogInformation("=== User Raw JSON ===\n{Json}", formattedJson);
                    
                    // 將 JSON 轉換為 Person 物件
                    // 同時保留原始 JSON 以便提取 costCenter 等可能不在強型別物件中的欄位
                    var person = JsonSerializer.Deserialize<Person>(personJsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (person != null)
                    {
                        people.Add(person);
                        peopleJson.Add(personJson);
                    }
                }
            }

            if (people.Any())
            {
                allPeople.AddRange(people);
                allPeopleJson.AddRange(peopleJson);
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

        return (allPeople, allPeopleJson);
    }

    private record Contact(
        string Id,
        string? Name,
        string? Email,
        string? Phone,
        string? Company,
        string? Department,
        string? Title,
        string? CostCenter
    );
}

