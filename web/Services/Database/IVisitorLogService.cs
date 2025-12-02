using web.Models;

namespace web.Services;

public interface IVisitorLogService
{
    Task<IEnumerable<VisitorLogResponse>> GetVisitorLogsAsync();
}

