using web.Models;

namespace web.Services;

public interface ICheckLogService
{
    Task<IEnumerable<CheckLogResponse>> GetCheckLogsAsync();
}

