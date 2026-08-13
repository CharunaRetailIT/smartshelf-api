using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Data;
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Models.user;
using TERMS_MOBILE_WEB_API.Interface;
using TERMS_MOBILE_WEB_API.Models;

namespace TERMS_MOBILE_WEB_API.Repository
{
    public class UserMasterRepository : IUserMaster
    {
        readonly DatabaseContext _dbContext = new();
        private readonly SmartShelfDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<UserMasterRepository> _logger;

        public UserMasterRepository(SmartShelfDbContext context, IWebHostEnvironment env, ILogger<UserMasterRepository> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        
        public List<UserMaster> GetUsersDetails(string DBConnectionString)
        {
            try
            {
               // return _dbContext.UserMasters.ToList();

                var contextOptions = new DbContextOptionsBuilder<DatabaseContext>().UseSqlServer(DBConnectionString).Options;

                using (var entities = new DatabaseContext(contextOptions))
                {
                    //get all countries
                    return entities.UserMaster.ToList();
                }
            }
            catch
            {
                throw;
            }
        }
     
        public ReturnUserDetails GetUserDetails(string DBConnectionString, string Username)
        {
            try
            {
                // return _dbContext.UserMasters.ToList();

                var contextOptions = new DbContextOptionsBuilder<DatabaseContext>().UseSqlServer(DBConnectionString).Options;

                using (var entities = new DatabaseContext(contextOptions))
                {                    //get all countries
                    //var x= entities.UserMaster.Where(x=>x.UserName == Username && x.IsActive ==true && x.IsDelete ==false).Select(a => new ReturnUserDetails { UserMasterID = a.UserMasterID, CompanyID = a.CompanyID, LocationID = a.LocationID, UserName = a.UserName, UserDescription = a.UserDescription, EmployeeCode = a.EmployeeCode, CreatedDate = a.CreatedDate }).FirstOrDefault();

                    var x = (from UM in entities.UserMaster join  C in entities.Company on  UM.CompanyID equals C.CompanyID
                                join L in entities.Location on UM.LocationID equals L.LocationID where UM.UserName.Equals(Username) && UM.IsActive.Equals(true ) && UM.IsDelete.Equals(false)

                                select new
                                {
                                   UM.UserMasterID,UM.CompanyID,UM.LocationID,UM.UserName,UM.UserDescription,
                                    UM.EmployeeCode , UM.CreatedDate,C.CompanyCode,C.CompanyName,L.LocationName,L.LocationCode,
                                }



                                )
                                .Select(a => new ReturnUserDetails { UserMasterID = a.UserMasterID, CompanyID = a.CompanyID, CompanyCode =a.CompanyCode, CompanyName =a.CompanyName, LocationName=a.LocationName, LocationCode =a.LocationCode, LocationID = a.LocationID, UserName = a.UserName, UserDescription = a.UserDescription, EmployeeCode = a.EmployeeCode, CreatedDate = a.CreatedDate }).FirstOrDefault();


                      



                    if (x==null)
                        return x;

                    AutoGenerateInfo _auto = entities.AutoGenerateInfo.Where(x => x.FormName == "FrmMobileAppPermission").FirstOrDefault();
                    ReturnUserPrivileges _UserPre = new ReturnUserPrivileges();
                    if (_auto != null)
                    {
                        _UserPre = entities.UserPrivileges.Where(A => A.TransactionRightsID == _auto.AutoGenerateInfoID && A.FormID == _auto.FormId && A.UserMasterID == x.UserMasterID).Select(a => new ReturnUserPrivileges { UserMasterID = a.UserMasterID, IsAccess = a.IsAccess, IsPause = a.IsPause, IsSave = a.IsSave, IsModify = a.IsModify, IsView = a.IsView, Layout = a.Layout }).FirstOrDefault();
                       
                    }
                    x.Return_UserPrivileges = _UserPre;
                    return x;  
                }
           
               
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public UserMaster IsValidUser(string Username, string Password, string DBConnectionString)
        {
            try
            {

                var contextOptions = new DbContextOptionsBuilder<DatabaseContext>().UseSqlServer(DBConnectionString).Options;

                using (var entities = new DatabaseContext(contextOptions))
                {
                    //get all countries
                    //return entities.UserMasters.ToList();


                    return entities.UserMaster.Where(X => X.UserName == Username && X.Password == Password).FirstOrDefault();
                    
                }



                
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<User> GetUserByEmployeeIdAsync(string employeeId)
        {
            return await _context.Users
                        .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        }

        public async Task<User> GetUserWithDetailsAsync(int userId)
        {
            return await _context.Users
                       .Include(u => u.Role)
                       .Include(u => u.Department)
                       .Include(u => u.Store)
                       .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            user.UpdatedDate = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<string> UpdateProfileImageAsync(int userId, IFormFile file)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {userId} not found");
                }

                // Validate file
                ValidateImageFile(file);

                // Generate unique filename
                var fileName = $"profile-{DateTime.Now.Ticks}{Path.GetExtension(file.FileName)}";

                // Define upload paths
                var uploadsFolder = Path.Combine(
                    _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                    "uploads", "profile-images");

                Directory.CreateDirectory(uploadsFolder); // Ensure folder exists

                var fullPath = Path.Combine(uploadsFolder, fileName);
                var relativePath = $"/uploads/profile-images/{fileName}";

                // Delete old image if exists
                DeleteOldProfileImage(user.ProfileImagePath);

                // Save new image
                using var stream = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(stream);

                // Update user profile
                user.ProfileImagePath = relativePath;
                user.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile image for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> CheckEmailExistsAsync(string email, int? excludeUserId = null)
        {
            var query = _context.Users.Where(u => u.Email == email);

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return await query.AnyAsync();
        }

        //profile support methods
        private void ValidateImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file uploaded");
            }

            // Validate file size (max 20MB)
            if (file.Length > 20_000_000)
            {
                throw new ArgumentException("File size must be less than 20MB");
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            var contentType = file.ContentType?.ToLower();

            if (string.IsNullOrEmpty(contentType) || !allowedTypes.Contains(contentType))
            {
                throw new ArgumentException("Only image files (JPEG, PNG, GIF, WEBP) are allowed");
            }
        }

        private void DeleteOldProfileImage(string oldImagePath)
        {
            if (string.IsNullOrEmpty(oldImagePath))
                return;

            try
            {
                var oldFullPath = Path.Combine(
                    _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                    oldImagePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

                if (File.Exists(oldFullPath))
                {
                    File.Delete(oldFullPath);
                }
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Failed to delete old profile image at {Path}", oldImagePath);
                // Don't throw - continue with new image upload
            }
        }
    }
    
}
