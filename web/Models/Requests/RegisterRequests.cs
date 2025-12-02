namespace web.Models;

public record SendOTPRequest(string Email);

public record SubmitRegistrationRequest(
    string Name,
    string Email,
    string Phone,
    string Company,
    string Otp,
    string Token
);

