using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Models;

namespace web.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FindAsync(email);
    }

    public async Task<User> CreateUserAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User?> UpdateUserAsync(string email, User user)
    {
        var existingUser = await _context.Users.FindAsync(email);
        if (existingUser == null)
        {
            return null;
        }

        existingUser.Username = user.Username;
        existingUser.Role = user.Role;
        
        await _context.SaveChangesAsync();
        return existingUser;
    }

    public async Task<bool> DeleteUserAsync(string email)
    {
        var user = await _context.Users.FindAsync(email);
        if (user == null)
        {
            return false;
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }
}

