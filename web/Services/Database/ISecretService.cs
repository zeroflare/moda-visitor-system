using web.Models;

namespace web.Services;

public interface ISecretService
{
    Task<IEnumerable<Secret>> GetAllSecretsAsync();
    Task<Secret?> GetSecretByIdAsync(string id);
    Task<Secret> CreateOrUpdateSecretAsync(string id, string value);
    Task<bool> DeleteSecretAsync(string id);
}

