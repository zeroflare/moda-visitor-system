using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Models;

namespace web.Services;

public class VisitorProfileService : IVisitorProfileService
{
    private readonly ApplicationDbContext _context;

    public VisitorProfileService(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<VisitorProfile?> GetVisitorProfileByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }
        return await _context.VisitorProfiles.FindAsync(email);
    }

    public async Task<VisitorProfile> CreateOrUpdateVisitorProfileAsync(VisitorProfile visitorProfile)
    {
        if (string.IsNullOrWhiteSpace(visitorProfile.Email))
        {
            throw new ArgumentException("Email cannot be null or empty", nameof(visitorProfile));
        }

        var existing = await _context.VisitorProfiles.FindAsync(visitorProfile.Email);
        if (existing != null)
        {
            // 更新現有記錄
            existing.Name = visitorProfile.Name;
            existing.Company = visitorProfile.Company;
            existing.Phone = visitorProfile.Phone;
            existing.Cid = visitorProfile.Cid;
            existing.UpdatedAt = DateTime.UtcNow;
            if (visitorProfile.ExpiresAt.HasValue)
            {
                existing.ExpiresAt = visitorProfile.ExpiresAt;
            }
            await _context.SaveChangesAsync();
            return existing;
        }
        else
        {
            // 創建新記錄
            visitorProfile.CreatedAt = DateTime.UtcNow;
            visitorProfile.UpdatedAt = DateTime.UtcNow;
            _context.VisitorProfiles.Add(visitorProfile);
            await _context.SaveChangesAsync();
            return visitorProfile;
        }
    }

    public async Task<bool> DeleteVisitorProfileAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var visitorProfile = await _context.VisitorProfiles.FindAsync(email);
        if (visitorProfile == null)
        {
            return false;
        }

        _context.VisitorProfiles.Remove(visitorProfile);
        await _context.SaveChangesAsync();
        return true;
    }
}

