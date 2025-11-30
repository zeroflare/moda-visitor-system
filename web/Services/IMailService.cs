namespace web.Services;

public interface IMailService
{
    Task SendOTPAsync(string email, string otp);
}

