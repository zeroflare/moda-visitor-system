using Google.Apis.Auth.OAuth2;

namespace console.Services;

public interface IGoogleAuthService
{
    Task<UserCredential> GetCredentialAsync(CancellationToken cancellationToken = default);
    Task SaveTokenAsync(UserCredential credential, CancellationToken cancellationToken = default);
}

