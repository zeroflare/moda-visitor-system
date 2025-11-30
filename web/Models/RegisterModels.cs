namespace web.Models;

public record SendOTPRequest(string Email);

public record SendOTPResponse(string Message);

public record SubmitRegistrationRequest(
    string Name,
    string Email,
    string Phone,
    string Company,
    string Otp
);

public record SubmitRegistrationResponse(
    string Message,
    string TransactionId,
    string QrcodeImage,
    string AuthUri
);

public record RegistrationResultResponse(string Message);

