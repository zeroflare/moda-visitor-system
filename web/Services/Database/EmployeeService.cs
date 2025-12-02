using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Models;

namespace web.Services;

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(ApplicationDbContext context, ILogger<EmployeeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Employee?> GetEmployeeByEmailAsync(string email)
    {
        return await _context.Employees.FirstOrDefaultAsync(e => e.Email == email);
    }

    public async Task<Employee> CreateOrUpdateEmployeeAsync(Employee employee)
    {
        var existing = await GetEmployeeByEmailAsync(employee.Email);
        
        if (existing != null)
        {
            // 更新現有員工
            existing.Name = employee.Name;
            existing.Dept = employee.Dept;
            existing.Costcenter = employee.Costcenter;
            existing.Title = employee.Title;
            await _context.SaveChangesAsync();
            return existing;
        }
        else
        {
            // 創建新員工
            // 如果沒有提供 ID，使用 Email 作為 ID
            if (string.IsNullOrWhiteSpace(employee.Id))
            {
                employee.Id = employee.Email;
            }
            
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            return employee;
        }
    }

    public async Task<int> SyncEmployeesAsync(IEnumerable<Employee> employees)
    {
        var syncedCount = 0;
        var createdCount = 0;
        var updatedCount = 0;

        foreach (var employee in employees)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(employee.Email))
                {
                    _logger.LogWarning("Skipping employee with empty email: {Name}", employee.Name);
                    continue;
                }

                // 優先使用 ID 查找（如果提供了 Google People ID）
                Employee? existing = null;
                if (!string.IsNullOrWhiteSpace(employee.Id))
                {
                    existing = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employee.Id);
                }
                
                // 如果通過 ID 找不到，則通過 Email 查找
                if (existing == null)
                {
                    existing = await GetEmployeeByEmailAsync(employee.Email);
                }
                
                if (existing != null)
                {
                    // 更新現有員工
                    // 如果新員工有 ID 但現有員工沒有，更新 ID
                    if (!string.IsNullOrWhiteSpace(employee.Id) && existing.Id != employee.Id)
                    {
                        // 檢查新 ID 是否已被其他員工使用
                        var idExists = await _context.Employees.AnyAsync(e => e.Id == employee.Id && e.Email != employee.Email);
                        if (!idExists)
                        {
                            existing.Id = employee.Id;
                        }
                    }
                    
                    existing.Name = employee.Name;
                    existing.Dept = employee.Dept;
                    existing.Costcenter = employee.Costcenter;
                    existing.Title = employee.Title;
                    updatedCount++;
                }
                else
                {
                    // 創建新員工
                    // 如果沒有提供 ID（不應該發生，因為 GooglePeopleService 會提供），使用 Email 作為後備
                    if (string.IsNullOrWhiteSpace(employee.Id))
                    {
                        employee.Id = employee.Email;
                    }
                    
                    // 檢查 ID 是否已存在
                    var idExists = await _context.Employees.AnyAsync(e => e.Id == employee.Id);
                    if (idExists)
                    {
                        _logger.LogWarning("Employee ID {Id} already exists for email {Email}, skipping", employee.Id, employee.Email);
                        continue;
                    }
                    
                    _context.Employees.Add(employee);
                    createdCount++;
                }
                
                syncedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing employee: {Email}", employee.Email);
            }
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Employee sync completed: {Synced} synced ({Created} created, {Updated} updated)", 
            syncedCount, createdCount, updatedCount);
        
        return syncedCount;
    }
}

