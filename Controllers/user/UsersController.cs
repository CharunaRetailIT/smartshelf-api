using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Context;
using TERMS_LOYALTY_API.Data;
using TERMS_LOYALTY_API.Models.DTOs.user;
using TERMS_LOYALTY_API.Models.user;
using TERMS_MOBILE_WEB_API.Models;

namespace TERMS_LOYALTY_API.Controllers
{
    [Route("api/user")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly SmartShelfDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<UsersController> _logger;
        public UsersController(SmartShelfDbContext db, IWebHostEnvironment env, ILogger<UsersController> logger)
        {
            _db = db;
            _env = env;
            _logger = logger;
        }

        private int? CurrentUserId =>
            int.TryParse(User.FindFirstValue("id"), out var id) ? id : null;

        /// <summary>
        /// Retrieves all users.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetAll()
        {
            var response = new HttpResponseData<object>();
            try
            {
                var users = await _db.Users.Include(u => u.Role)
                                           .Select(u => new
                                           {
                                               u.Id,
                                               u.EmployeeId,
                                               u.FirstName,
                                               u.LastName,
                                               u.Email,
                                               u.IsActive,
                                               u.DepartmentId,
                                               Department = u.Department.Name,
                                               u.RoleId,
                                               Role = u.Role.Name,
                                               u.ProfileImagePath,
                                               u.Address1,
                                               u.Address2,
                                               u.Address3,
                                               u.CreatedDate
                                           }).OrderByDescending(u => u.CreatedDate)
                                           .ToListAsync();

                response.Success = true;
                response.Message = "Users retrieved successfully";
                response.Results = users.Cast<object>().ToList();
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                response.Success = false;
                response.Message = "Failed to retrieve users";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return StatusCode(500, response);
            }
        }


        //[HttpGet]
        ////[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> GetAll()
        //{
        //    var users = await _db.Users.Include(u => u.Role)
        //                               .Select(u => new
        //                               {
        //                                   u.Id,
        //                                   u.EmployeeId,
        //                                   u.FirstName,
        //                                   u.LastName,
        //                                   u.Email,
        //                                   u.IsActive,
        //                                   u.DepartmentId,
        //                                   Department = u.Department.Name,
        //                                   u.RoleId,
        //                                   Role = u.Role.Name,
        //                                   u.ProfileImagePath,
        //                                   u.Address1,
        //                                   u.Address2,
        //                                   u.Address3,
        //                                   u.CreatedDate
        //                               })
        //                               .ToListAsync();
        //    return Ok(users);
        //}



        /// <summary>
        /// Retrieves a user by ID.
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetById(int id)
        {
            var response = new HttpResponseData<object>();
            try
            {
                var user = await _db.Users.Include(u => u.Role)
                                          .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    response.Success = false;
                    response.Message = $"User with ID {id} not found";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                response.Success = true;
                response.Message = "User retrieved successfully";
                response.Result = new
                {
                    user.Id,
                    user.EmployeeId,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.DepartmentId,
                    Role = user.Role?.Name,
                    user.ProfileImagePath,
                    user.Address1,
                    user.Address2,
                    user.Address3
                };
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user ID {id}");
                response.Success = false;
                response.Message = "Failed to retrieve user";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return StatusCode(500, response);
            }
        }


        /// <summary>
        /// Updates an existing user.
        /// </summary>
        [HttpPut("{userId?}")]
        [ProducesResponseType(typeof(HttpResponseData<User>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<User>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<User>), 401)]
        [ProducesResponseType(typeof(HttpResponseData<User>), 403)]
        [ProducesResponseType(typeof(HttpResponseData<User>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<User>), 500)]
        public async Task<IActionResult> UpdateUser(int? userId, [FromBody] UpdateUserDto dto)
        {
            var response = new HttpResponseData<User>();
            if (!userId.HasValue || userId <= 0)
            {
                response.Success = false;
                response.Message = "Valid User ID is required";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            // An Admin may edit any user; everyone else may edit only their own record.
            var callerId = CurrentUserId;
            if (!callerId.HasValue)
            {
                response.Success = false;
                response.Message = "Unauthorized";
                response.ResponsCode = 401;
                return Unauthorized(response);
            }

            if (!User.IsInRole("Admin") && callerId.Value != userId.Value)
            {
                _logger.LogWarning($"User {callerId.Value} attempted to update user ID {userId}");
                response.Success = false;
                response.Message = "You can only update your own profile";
                response.ResponsCode = 403;
                return StatusCode(403, response);
            }

            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
                if (user == null)
                {
                    response.Success = false;
                    response.Message = $"User with ID {userId} not found";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                if (!string.IsNullOrEmpty(dto.EmployeeId)) user.EmployeeId = dto.EmployeeId;
                if (!string.IsNullOrEmpty(dto.FirstName)) user.FirstName = dto.FirstName;
                if (!string.IsNullOrEmpty(dto.LastName)) user.LastName = dto.LastName;
                if (!string.IsNullOrEmpty(dto.Email)) user.Email = dto.Email;
                if (dto.Department.HasValue) user.DepartmentId = dto.Department.Value;
                if (!string.IsNullOrEmpty(dto.Address1)) user.Address1 = dto.Address1;
                if (!string.IsNullOrEmpty(dto.Address2)) user.Address2 = dto.Address2;
                if (!string.IsNullOrEmpty(dto.Address3)) user.Address3 = dto.Address3;

                await _db.SaveChangesAsync();

                response.Success = true;
                response.Message = "User updated successfully";
                response.Result = user;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Error updating user ID {userId}");
                response.Success = false;
                response.Message = "Database update failed";
                response.Error = ex.InnerException?.Message ?? ex.Message;
                response.ResponsCode = 500;
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Assigns a role to a user.
        /// </summary>
        [HttpPost("assign-role")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(HttpResponseData<string>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<string>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<string>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<string>), 500)]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto dto)
        {
            var response = new HttpResponseData<string>();
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId);
                if (user == null)
                {
                    response.Success = false;
                    response.Message = "User not found";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == dto.Role);
                if (role == null)
                {
                    response.Success = false;
                    response.Message = "Invalid role";
                    response.ResponsCode = 400;
                    return BadRequest(response);
                }

                user.RoleId = role.Id;
                await _db.SaveChangesAsync();

                response.Success = true;
                response.Message = $"Role set to {role.Name}";
                response.Result = role.Name;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error assigning role to user ID {dto.UserId}");
                response.Success = false;
                response.Message = "Failed to assign role";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Uploads profile image for the current user.
        /// </summary>
        [HttpPost("me/profile-image")]
        [RequestSizeLimit(20_000_000)]
        [ProducesResponseType(typeof(HttpResponseData<string>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<string>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<string>), 401)]
        [ProducesResponseType(typeof(HttpResponseData<string>), 500)]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            var response = new HttpResponseData<string>();
            try
            {
                if (file == null || file.Length == 0)
                {
                    response.Success = false;
                    response.Message = "No file uploaded";
                    response.ResponsCode = 400;
                    return BadRequest(response);
                }

                var userId = CurrentUserId;
                if (!userId.HasValue)
                {
                    response.Success = false;
                    response.Message = "Unauthorized";
                    response.ResponsCode = 401;
                    return Unauthorized(response);
                }

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
                if (user == null)
                {
                    response.Success = false;
                    response.Message = "User not found";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                var folder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "profile-images");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var ext = Path.GetExtension(file.FileName);
                var fileName = $"{user.Id}_{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(folder, fileName);

                using (var stream = System.IO.File.Create(fullPath))
                {
                    await file.CopyToAsync(stream);
                }

                user.ProfileImagePath = $"/profile-images/{fileName}";
                await _db.SaveChangesAsync();

                response.Success = true;
                response.Message = "Profile image uploaded successfully";
                response.Result = user.ProfileImagePath;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile image");
                response.Success = false;
                response.Message = "Failed to upload profile image";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return StatusCode(500, response);
            }
        }


        /// <summary>
        /// Retrieves all roles.
        /// </summary>
        [HttpGet("roles")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetAllRoles()
        {
            var response = new HttpResponseData<object>();
            try
            {
                var roles = await _db.Roles.ToListAsync();
                if (roles == null || !roles.Any())
                {
                    response.Success = false;
                    response.Message = "No roles found";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                response.Success = true;
                response.Message = "Roles retrieved successfully";
                response.Result = roles.Select(role => new Role
                {
                    Id = role.Id,
                    Name = role.Name,
                    IsActive = role.IsActive,
                    CreatedDate = role.CreatedDate,
                    UpdatedDate = role.UpdatedDate
                }).ToList();
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                response.Success = false;
                response.Message = "Failed to retrieve roles";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Retrieves all departments.
        /// </summary>
        [HttpGet("departments")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetAllDepartments()
        {
            var response = new HttpResponseData<object>();
            try
            {
                var departments = await _db.Departments.ToListAsync();
                if (departments == null || !departments.Any())
                {
                    response.Success = false;
                    response.Message = "No departments found";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                response.Success = true;
                response.Message = "Departments retrieved successfully";
                response.Result = departments.Select(d => new Department
                {
                    Id = d.Id,
                    Name = d.Name,
                    IsActive = d.IsActive,
                    CreatedDate = d.CreatedDate,
                    UpdatedDate = d.UpdatedDate
                }).ToList();
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving departments");
                response.Success = false;
                response.Message = "Failed to retrieve departments";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Deletes (deactivates) a user by ID.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 500)]
        public async Task<IActionResult> Delete(int id)
        {
            var response = new HttpResponseData<bool>();
            try
            {
                var user = await _db.Users.FindAsync(id);
                if (user == null)
                {
                    response.Success = false;
                    response.Message = "User not found";
                    response.ResponsCode = 404;
                    response.Result = false;
                    return NotFound(response);
                }

                if (!user.IsActive)
                {
                    response.Success = false;
                    response.Message = "User is already deactivated";
                    response.ResponsCode = 400;
                    response.Result = false;
                    return BadRequest(response);
                }

                user.IsActive = false;
                user.UpdatedDate = DateTime.UtcNow;
                _db.Entry(user).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                response.Success = true;
                response.Message = "User deleted successfully";
                response.ResponsCode = 200;
                response.Result = true;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user ID {id}");
                response.Success = false;
                response.Message = "An error occurred while deleting the user";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                response.Result = false;
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Restores (reactivates) a user by ID.
        /// </summary>
        [HttpPut("{id}/restore")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 500)]
        public async Task<IActionResult> Restore(int id)
        {
            var response = new HttpResponseData<bool>();
            try
            {
                var user = await _db.Users.FindAsync(id);
                if (user == null)
                {
                    response.Success = false;
                    response.Message = "User not found";
                    response.ResponsCode = 404;
                    response.Result = false;
                    return NotFound(response);
                }

                if (user.IsActive)
                {
                    response.Success = false;
                    response.Message = "User is already active";
                    response.ResponsCode = 400;
                    response.Result = false;
                    return BadRequest(response);
                }

                user.IsActive = true;
                user.UpdatedDate = DateTime.UtcNow;
                _db.Entry(user).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                response.Success = true;
                response.Message = "User restored successfully";
                response.ResponsCode = 200;
                response.Result = true;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restoring user ID {id}");
                response.Success = false;
                response.Message = "An error occurred while restoring the user";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                response.Result = false;
                return StatusCode(500, response);
            }
        }
    }
}

