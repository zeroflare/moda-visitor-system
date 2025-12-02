namespace web.Services;

public interface IMailService
{
    Task SendOTPAsync(string email, string otp);
    Task SendRegisterInvitationAsync(string email, string token, string registerUrl);
    Task SendCheckinNotificationAsync(string inviterEmail, string visitorName, string visitorEmail, string meetingName, string meetingRoom, string checkinTime);
}

