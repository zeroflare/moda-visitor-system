using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Models;

namespace web.Services;

public class SecretService : ISecretService
{
    private readonly ApplicationDbContext _context;

    public SecretService(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<Secret>> GetAllSecretsAsync()
    {
        return await _context.Secrets.ToListAsync();
    }

    public async Task<Secret?> GetSecretByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }
        return await _context.Secrets.FindAsync(id);
    }

    public async Task<Secret> CreateOrUpdateSecretAsync(string id, string value)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Secret ID cannot be null or empty", nameof(id));
        }

        var existingSecret = await _context.Secrets.FindAsync(id);
        if (existingSecret != null)
        {
            existingSecret.Value = value;
            await _context.SaveChangesAsync();
            return existingSecret;
        }
        else
        {
            var newSecret = new Secret
            {
                Id = id,
                Value = value
            };
            _context.Secrets.Add(newSecret);
            await _context.SaveChangesAsync();
            return newSecret;
        }
    }

    public async Task<bool> DeleteSecretAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        var secret = await _context.Secrets.FindAsync(id);
        if (secret == null)
        {
            return false;
        }

        _context.Secrets.Remove(secret);
        await _context.SaveChangesAsync();
        return true;
    }
}

