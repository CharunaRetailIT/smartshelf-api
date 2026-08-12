using System;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Models.DTOs.user;
using TERMS_LOYALTY_API.Models.user;

namespace TERMS_LOYALTY_API.Interface
{
    public interface IAuth
    {
        Task<bool> EmployeeIdExistsAsync(string employeeId);
        Task<User> GetUserByEmployeeIdAsync(string employeeId, bool includeRole = false);
        Task<User> GetUserByIdAsync(Guid id, bool includeRole = false);
        Task<User> CreateUserAsync(RegisterDto request);
    }
}
