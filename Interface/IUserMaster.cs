using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Models.user;
using TERMS_MOBILE_WEB_API.Models;

namespace TERMS_MOBILE_WEB_API.Interface
{
    public interface IUserMaster
    {

        public List<UserMaster> GetUsersDetails(string DBConnectionString);
        public ReturnUserDetails GetUserDetails(string DBConnectionString, string Username);
        public UserMaster IsValidUser(string Username, string Password, string DBConnectionString);

        //Handler methods for SmartShelf users
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserByEmployeeIdAsync(string employeeId);
        Task<User> GetUserWithDetailsAsync(int userId);
        Task<User> UpdateUserAsync(User user);
        Task<string> UpdateProfileImageAsync(int userId, IFormFile file);
        Task<bool> CheckEmailExistsAsync(string email, int? excludeUserId = null);

    }
}
