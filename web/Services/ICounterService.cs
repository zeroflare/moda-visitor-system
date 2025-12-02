using web.Models;

namespace web.Services;

public interface ICounterService
{
    Task<IEnumerable<Counter>> GetAllCountersAsync();
    Task<Counter?> GetCounterByIdAsync(string id);
    Task<Counter> CreateCounterAsync(Counter counter);
    Task<Counter?> UpdateCounterAsync(string id, Counter counter);
    Task<bool> DeleteCounterAsync(string id);
}

