using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Models;

namespace web.Services;

public class CounterService : ICounterService
{
    private readonly ApplicationDbContext _context;

    public CounterService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Counter>> GetAllCountersAsync()
    {
        return await _context.Counters.ToListAsync();
    }

    public async Task<Counter?> GetCounterByIdAsync(string id)
    {
        return await _context.Counters.FindAsync(id);
    }

    public async Task<Counter> CreateCounterAsync(Counter counter)
    {
        _context.Counters.Add(counter);
        await _context.SaveChangesAsync();
        return counter;
    }

    public async Task<Counter?> UpdateCounterAsync(string id, Counter counter)
    {
        var existingCounter = await _context.Counters.FindAsync(id);
        if (existingCounter == null)
        {
            return null;
        }

        existingCounter.Name = counter.Name;
        await _context.SaveChangesAsync();
        return existingCounter;
    }

    public async Task<bool> DeleteCounterAsync(string id)
    {
        var counter = await _context.Counters.FindAsync(id);
        if (counter == null)
        {
            return false;
        }

        _context.Counters.Remove(counter);
        await _context.SaveChangesAsync();
        return true;
    }
}

