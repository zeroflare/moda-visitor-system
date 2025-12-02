namespace web.Services.Scheduled;

public interface IRegistrationInvitationService
{
    Task SendInvitationsToTomorrowVisitorsAsync(CancellationToken cancellationToken = default);
}

