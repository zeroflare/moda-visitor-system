namespace web.Models;

public record SendOTPResponse(string Message);

public record SubmitRegistrationResponse(
    string Message,
    string TransactionId,
    string QrcodeImage,
    string AuthUri
);

public record RegistrationResultResponse(string Message);

