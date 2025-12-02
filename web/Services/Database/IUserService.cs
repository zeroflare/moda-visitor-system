using web.Models;

namespace web.Services;

public interface IUserService
{
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User?> GetUserByEmailAsync(string email);
    Task<User> CreateUserAsync(User user);
    Task<User?> UpdateUserAsync(string email, User user);
    Task<bool> DeleteUserAsync(string email);
}

