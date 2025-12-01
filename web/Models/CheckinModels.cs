namespace web.Models;

public record QRCodeResponse(string QrcodeImage, string AuthUri);

public record CheckinResultResponse(
    string InviterEmail,
    string InviterName,
    string InviterDept,
    string InviterTitle,
    string? VisitorEmail,
    string? VisitorName,
    string? VisitorDept,
    string? VisitorPhone,
    string MeetingTime,
    string MeetingName,
    string MeetingRoom
);

public record CounterInfoResponse(string Name, string Description);

