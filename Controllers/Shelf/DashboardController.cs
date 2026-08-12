using log4net.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Interface;
using TERMS_LOYALTY_API.Repository;
using TERMS_MOBILE_WEB_API.Models;
using static TERMS_LOYALTY_API.DTOs.shelf.DashboardDto;

namespace TERMS_LOYALTY_API.Controllers.Shelf
{
    [Route("api/dashboard")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboard _dashboardRepo;
        //private readonly IUser _currentUserService;
        private readonly ILogger<DashboardController> _logger;
        public DashboardController(IDashboard dashboard, ILogger<DashboardController> logger)
        {
            _dashboardRepo = dashboard;
            _logger = logger;
        }


        /// <summary>
        /// Gets dashboard data for the logged-in user
        /// Uses the existing stored procedure via repository
        /// </summary>
        [HttpGet("data")]
        [ProducesResponseType(typeof(HttpResponseData<DashboardDto.DashboardDataDto>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 401)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<ActionResult<HttpResponseData<DashboardDto.DashboardDataDto>>> GetDashboardData()
        {
            var response = new HttpResponseData<DashboardDto.DashboardDataDto>();

            try
            {
                // Get current user ID from JWT token claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    response.Success = false;
                    response.Message = "User not authenticated";
                    response.ResponsCode = 401;
                    return Unauthorized(response);
                }

                // Use the existing repository method
                var dashboardData = await _dashboardRepo.GetDashboardDataAsync(userId);

                response.Success = true;
                response.Message = "Dashboard data loaded successfully";
                response.ResponsCode = 200;
                response.Result = dashboardData;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");

                response.Success = false;
                response.Message = $"Error loading dashboard data: {ex.Message}";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Gets dashboard summary only (quick stats)
        /// Uses the same stored procedure but extracts only summary
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(HttpResponseData<DashboardDto.DashboardSummaryDto>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 401)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<ActionResult<HttpResponseData<DashboardDto.DashboardSummaryDto>>> GetDashboardSummary()
        {
            var response = new HttpResponseData<DashboardDto.DashboardSummaryDto>();

            try
            {
                // Get current user ID from JWT token claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    response.Success = false;
                    response.Message = "User not authenticated";
                    response.ResponsCode = 401;
                    return Unauthorized(response);
                }

                // Use the existing repository method
                var dashboardData = await _dashboardRepo.GetDashboardDataAsync(userId);

                response.Success = true;
                response.Message = "Dashboard summary loaded successfully";
                response.ResponsCode = 200;
                response.Result = dashboardData.Summary;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard summary");

                response.Success = false;
                response.Message = $"Error loading dashboard summary: {ex.Message}";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Gets only recent shelves from dashboard
        /// </summary>
        [HttpGet("recent-shelves")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 401)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<ActionResult<HttpResponseData<object>>> GetRecentShelves()
        {
            var response = new HttpResponseData<object>();

            try
            {
                // Get current user ID from JWT token claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    response.Success = false;
                    response.Message = "User not authenticated";
                    response.ResponsCode = 401;
                    return Unauthorized(response);
                }

                // Use the existing repository method
                var dashboardData = await _dashboardRepo.GetDashboardDataAsync(userId);

                response.Success = true;
                response.Message = "Recent shelves loaded successfully";
                response.ResponsCode = 200;
                response.Result = dashboardData.RecentShelves;
                response.ResponsCode = dashboardData.RecentShelves?.Count ?? 0;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recent shelves");

                response.Success = false;
                response.Message = $"Error loading recent shelves: {ex.Message}";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Gets only recent activities from dashboard
        /// </summary>
        [HttpGet("recent-activities")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 401)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<ActionResult<HttpResponseData<object>>> GetRecentActivities()
        {
            var response = new HttpResponseData<object>();

            try
            {
                // Get current user ID from JWT token claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    response.Success = false;
                    response.Message = "User not authenticated";
                    response.ResponsCode = 401;
                    return Unauthorized(response);
                }

                // Use the existing repository method
                var dashboardData = await _dashboardRepo.GetDashboardDataAsync(userId);

                response.Success = true;
                response.Message = "Recent activities loaded successfully";
                response.ResponsCode = 200;
                response.Result = dashboardData.RecentActivities;
                response.ResponsCode = dashboardData.RecentActivities?.Count ?? 0;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recent activities");

                response.Success = false;
                response.Message = $"Error loading recent activities: {ex.Message}";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Gets user-specific dashboard with additional user info
        /// Combines dashboard data with user profile
        /// </summary>
        [HttpGet("user-dashboard")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 401)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<ActionResult<HttpResponseData<object>>> GetUserDashboard()
        {
            var response = new HttpResponseData<object>();

            try
            {
                // Get user info from JWT token claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
                var userEmail = User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
                var firstName = User.Claims.FirstOrDefault(c => c.Type == "firstName")?.Value;
                var lastName = User.Claims.FirstOrDefault(c => c.Type == "lastName")?.Value;
                var employeeId = User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName)?.Value;
                var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
                var departmentId = User.Claims.FirstOrDefault(c => c.Type == "departmentId")?.Value;
                var profileImage = User.Claims.FirstOrDefault(c => c.Type == "profileImagePath")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    response.Success = false;
                    response.Message = "User not authenticated";
                    response.ResponsCode = 401;
                    return Unauthorized(response);
                }

                // Get dashboard data
                var dashboardData = await _dashboardRepo.GetDashboardDataAsync(userId);

                // Create combined response
                var userDashboard = new
                {
                    UserInfo = new
                    {
                        Id = userId,
                        FirstName = firstName,
                        LastName = lastName,
                        Email = userEmail,
                        EmployeeId = employeeId,
                        Role = role,
                        DepartmentId = departmentId,
                        ProfileImagePath = profileImage
                    },
                    Dashboard = dashboardData
                };

                response.Success = true;
                response.Message = "User dashboard loaded successfully";
                response.ResponsCode = 200;
                response.Result = userDashboard;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user dashboard");

                response.Success = false;
                response.Message = $"Error loading user dashboard: {ex.Message}";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Public dashboard data (doesn't require authentication)
        /// Shows general stats without user-specific filtering
        /// </summary>
        [HttpGet("public-data")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(HttpResponseData<DashboardDto.DashboardSummaryDto>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<ActionResult<HttpResponseData<DashboardDto.DashboardSummaryDto>>> GetPublicDashboardData()
        {
            var response = new HttpResponseData<DashboardDto.DashboardSummaryDto>();

            try
            {
                // Pass null userId to get general stats (not user-specific)
                var dashboardData = await _dashboardRepo.GetDashboardDataAsync(null);

                response.Success = true;
                response.Message = "Public dashboard data loaded successfully";
                response.ResponsCode = 200;
                response.Result = dashboardData.Summary;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading public dashboard data");

                response.Success = false;
                response.Message = $"Error loading public dashboard data: {ex.Message}";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Health check endpoint for dashboard
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        public ActionResult<HttpResponseData<object>> HealthCheck()
        {
            var response = new HttpResponseData<object>
            {
                Success = true,
                Message = "Dashboard API is running",
                ResponsCode = 200,
                Result = new
                {
                    Status = "OK",
                    Timestamp = DateTime.UtcNow,
                    Version = "1.0.0"
                }
            };

            return Ok(response);
        }
    }
}
