using web.Models;

namespace web.Services;

public interface ITwdiwService
{
    Task<QRCodeResponse> GetQRCodeAsync(string transactionId, string? counter = null);
    Task<CheckinResultResponse> GetCheckinResultAsync(string transactionId);
    Task<SubmitRegistrationResponse> SubmitRegistrationAsync(SubmitRegistrationRequest request);
    Task<object> GetRegistrationResultAsync(string transactionId);
}

