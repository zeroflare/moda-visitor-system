using web.Models;

namespace web.Services;

public interface IEmployeeService
{
    Task<Employee?> GetEmployeeByEmailAsync(string email);
    Task<Employee> CreateOrUpdateEmployeeAsync(Employee employee);
    Task<int> SyncEmployeesAsync(IEnumerable<Employee> employees);
}

