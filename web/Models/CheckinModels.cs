namespace web.Models;

public record QRCodeResponse(string QrcodeImage, string AuthUri);

public record CheckinResultResponse(
    string InviterEmail,
    string InviterName,
    string InviterDept,
    string InviterTitle,
    string? VistorEmail,
    string? VistorName,
    string? VistorDept,
    string? VistorPhone,
    string MeetingTime,
    string MeetingRoom
);

public record CounterInfoResponse(string Name, string Description);

