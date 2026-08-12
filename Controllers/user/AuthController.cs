using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Context;
using TERMS_LOYALTY_API.Data;
using TERMS_LOYALTY_API.DTOs.user;
using TERMS_LOYALTY_API.Interface;
using TERMS_LOYALTY_API.Models.DTOs.user;
using TERMS_LOYALTY_API.Models.user;
using TERMS_LOYALTY_API.Services;
using TERMS_LOYALTY_API.Shared.Helpers;
using TERMS_MOBILE_WEB_API.Interface;
using TERMS_MOBILE_WEB_API.Models;

namespace TERMS_LOYALTY_API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly SmartShelfDbContext _db;
        private readonly IJwtService _jwt;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly ILogger<AuthController> _logger;
        private readonly IUserMaster _userRepo;
        public AuthController(SmartShelfDbContext db, IJwtService jwt,ILogger<AuthController> logger, IUserMaster userRepo)
        {
            _db = db;
            _jwt = jwt;
            _passwordHasher = new PasswordHasher<User>();
            _logger = logger;
            _userRepo = userRepo;
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var response = new HttpResponseData<object>();
            try
            {
                if (await _db.Users.AnyAsync(u => u.EmployeeId == dto.EmployeeId))
                {
                    response.Success = false;
                    response.Message = "EmployeeId already exists";
                    response.ResponsCode = 400;
                    return BadRequest(response);
                }

                var user = new User
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    UserName = dto.EmployeeId,
                    EmployeeId = dto.EmployeeId,
                    Email = dto.Email,
                    DepartmentId = dto.Department,
                    RoleId = dto.RoleId.HasValue ? dto.RoleId.Value : 4, // Default role = User
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);
                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                response.Success = true;
                response.Message = "Registered successfully";
                response.Result = new
                {
                    user.Id,
                    user.EmployeeId,
                    user.Email
                };
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user");
                response.Success = false;
                response.Message = "Failed to register user";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 401)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var response = new HttpResponseData<object>();
            try
            {
                var user = await _db.Users.Include(u => u.Role)
                                          .FirstOrDefaultAsync(u => u.EmployeeId == dto.UserName);

                if (user == null)
                {
                    response.Success = false;
                    response.Message = "Invalid credentials";
                    response.ResponsCode = 401;
                    return Unauthorized(response);
                }

                var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
                if (result == PasswordVerificationResult.Failed)
                {
                    response.Success = false;
                    response.Message = "Invalid credentials";
                    response.ResponsCode = 401;
                    return Unauthorized(response);
                }

                var roles = new[] { user.Role?.Name ?? "User" };
                var token = _jwt.CreateToken(user, roles);

                response.Success = true;
                response.Message = "Login successful";
                response.Result = new
                {
                    token,
                    user = new
                    {
                        user.Id,
                        user.EmployeeId,
                        user.FirstName,
                        user.LastName,
                        user.Email,
                        user.DepartmentId,
                        user.ProfileImagePath,
                        roles
                    }
                };
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                response.Success = false;
                response.Message = "Failed to login";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return StatusCode(500, response);
            }
        }


        /// <summary>
        /// Retrieves the currently logged-in user's profile.
        /// </summary>
        //[HttpGet("me")]
        //[Authorize]
        //[ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        //[ProducesResponseType(typeof(HttpResponseData<object>), 401)]
        //[ProducesResponseType(typeof(HttpResponseData<object>), 404)]
        //[ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        //public async Task<IActionResult> Me()
        //{
        //    var response = new HttpResponseData<object>();
        //    try
        //    {
        //        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
        //        if (string.IsNullOrEmpty(userIdClaim))
        //        {
        //            response.Success = false;
        //            response.Message = "Unauthorized";
        //            response.ResponsCode = 401;
        //            return Unauthorized(response);
        //        }

        //        var user = await _db.Users.Include(u => u.Role)
        //                                  .FirstOrDefaultAsync(u => u.Id.ToString() == userIdClaim);

        //        if (user == null)
        //        {
        //            response.Success = false;
        //            response.Message = "User not found";
        //            response.ResponsCode = 404;
        //            return NotFound(response);
        //        }

        //        response.Success = true;
        //        response.Message = "User profile retrieved successfully";
        //        response.Result = new
        //        {
        //            user.Id,
        //            user.EmployeeId,
        //            user.FirstName,
        //            user.LastName,
        //            user.Email,
        //            user.DepartmentId,
        //            user.ProfileImagePath,
        //            roles = new[] { user.Role?.Name ?? "User" }
        //        };
        //        response.ResponsCode = 200;
        //        return Ok(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error retrieving current user profile");
        //        response.Success = false;
        //        response.Message = "Failed to retrieve profile";
        //        response.Error = ex.Message;
        //        response.ResponsCode = 500;
        //        return StatusCode(500, response);
        //    }
        //}

        // Update Me() method
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 401)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> Me()
        {
            var response = new HttpResponseData<object>();
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    response.Success = false;
                    response.Message = "Unauthorized";
                    response.ResponsCode = 401;
                    return Unauthorized(response);
                }

                var user = await _userRepo.GetUserWithDetailsAsync(userId);
                if (user == null)
                {
                    response.Success = false;
                    response.Message = "User not found";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                var result = new UserProfileDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserName = user.UserName,
                    EmployeeId = user.EmployeeId,
                    Email = user.Email,
                    Address1 = user.Address1,
                    Address2 = user.Address2,
                    Address3 = user.Address3,
                    ProfileImageUrl = Request.ToAbsoluteUrl(user.ProfileImagePath),

                    Role = user.Role?.Name ?? "",
                    Department = user.Department?.Name ?? ""
                };

                response.Success = true;
                response.Message = "User profile retrieved successfully";
                response.Result = result;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user profile");
                response.Success = false;
                response.Message = "Failed to retrieve profile";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return StatusCode(500, response);
            }
        }

        // Update UploadProfileImage method
        [HttpPost("me/profile-image")]
        [Authorize]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 401)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            var response = new HttpResponseData<object>();
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    response.Success = false;
                    response.Message = "Unauthorized";
                    response.ResponsCode = 401;
                    return Unauthorized(response);
                }

                var profileImagePath = await _userRepo.UpdateProfileImageAsync(userId, file);

                // Get updated user details
                var user = await _userRepo.GetUserWithDetailsAsync(userId);

                response.Success = true;
                response.Message = "Profile image uploaded successfully";
                response.Result = new
                {
                    profileImagePath,
                    profileImageUrl = $"{Request.Scheme}://{Request.Host}{profileImagePath}",
                    user = user != null ? MapToProfileDto(user, Request) : null
                };
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                // Handle validation errors
                response.Success = false;
                response.Message = ex.Message;
                response.ResponsCode = 400;
                return BadRequest(response);
            }
            catch (KeyNotFoundException ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.ResponsCode = 404;
                return NotFound(response);
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

        // Update UpdateProfile method
        [HttpPut("me")]
        [Authorize]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 401)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var response = new HttpResponseData<object>();
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    response.Success = false;
                    response.Message = "Unauthorized";
                    response.ResponsCode = 401;
                    return Unauthorized(response);
                }

                var user = await _userRepo.GetUserWithDetailsAsync(userId);
                if (user == null)
                {
                    response.Success = false;
                    response.Message = "User not found";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                // Check email uniqueness if changing email
                if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
                {
                    var emailExists = await _userRepo.CheckEmailExistsAsync(dto.Email, userId);
                    if (emailExists)
                    {
                        response.Success = false;
                        response.Message = "Email is already in use by another account";
                        response.ResponsCode = 400;
                        return BadRequest(response);
                    }
                    user.Email = dto.Email;
                }

                // Update user properties
                user.FirstName = dto.FirstName ?? user.FirstName;
                user.LastName = dto.LastName ?? user.LastName;
                user.Address1 = dto.Address1;
                user.Address2 = dto.Address2;
                user.Address3 = dto.Address3;

                // Save changes through repository
                var updatedUser = await _userRepo.UpdateUserAsync(user);

                response.Success = true;
                response.Message = "Profile updated successfully";
                response.Result = MapToProfileDto(updatedUser, Request);
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                response.Success = false;
                response.Message = "Failed to update profile";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return StatusCode(500, response);
            }
        }

        // Helper method to map User to DTO
        private object MapToProfileDto(User user, HttpRequest request)
        {
            return new
            {
                user.Id,
                user.EmployeeId,
                user.FirstName,
                user.LastName,
                user.Email,
                user.DepartmentId,
                Department = user.Department?.Name,
                user.RoleId,
                Role = user.Role?.Name,
                ProfileImageUrl = string.IsNullOrEmpty(user.ProfileImagePath)
            ? null
            : request.ToAbsoluteUrl(user.ProfileImagePath),
                user.Address1,
                user.Address2,
                user.Address3
            };
        }

    }
}


