using web.Models;

namespace web.Services;

public interface IRegisterTokenService
{
    Task<IEnumerable<RegisterToken>> GetAllRegisterTokensAsync();
    Task<RegisterToken?> GetRegisterTokenByIdAsync(string id);
    Task<IEnumerable<RegisterToken>> GetRegisterTokensByEmailAsync(string visitorEmail);
    Task<RegisterToken> CreateRegisterTokenAsync(RegisterToken registerToken);
    Task<RegisterToken?> UpdateRegisterTokenAsync(string id, RegisterToken registerToken);
    Task<bool> DeleteRegisterTokenAsync(string id);
}

