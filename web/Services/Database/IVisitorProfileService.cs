using web.Models;

namespace web.Services;

public interface IVisitorProfileService
{
    Task<VisitorProfile?> GetVisitorProfileByEmailAsync(string email);
    Task<VisitorProfile> CreateOrUpdateVisitorProfileAsync(VisitorProfile visitorProfile);
    Task<bool> DeleteVisitorProfileAsync(string email);
}

