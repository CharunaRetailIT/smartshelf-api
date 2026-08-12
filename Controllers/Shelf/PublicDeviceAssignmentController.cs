using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Data;
using TERMS_LOYALTY_API.Models.shelf;
using TERMS_LOYALTY_API.SignalRHubs;

namespace TERMS_LOYALTY_API.Controllers.Shelf
{
    [Route("api/public/device-assignments")]
    [ApiController]
    [AllowAnonymous]
    public class PublicDeviceAssignmentController : ControllerBase
    {
        private readonly SmartShelfDbContext _context;
        private readonly IHubContext<DeviceAssignmentHub> _hubContext;
        private readonly ILogger<PublicDeviceAssignmentController> _logger;

        public PublicDeviceAssignmentController(SmartShelfDbContext context, IHubContext<DeviceAssignmentHub> hubContext, ILogger<PublicDeviceAssignmentController> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }
        /// <summary>
        /// Standardized response method
        /// </summary>
        private IActionResult ApiResponse(bool success, string message, object data = null, string error = null, int statusCode = 200)
        {
            var response = new
            {
                success,
                message,
                data,
                error
            };

            return statusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(statusCode, response)
            };
        }

        /// <summary>
        /// Get assignments for a device type - PUBLIC (no auth)
        /// </summary>
        [HttpGet("{deviceType}")]
        public async Task<IActionResult> GetAssignments(string deviceType, [FromQuery] long? storeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceType))
                {
                    return ApiResponse(false, "Device type is required", null, "DeviceType parameter is missing", 400);
                }

                var assignments = await _context.DeviceAssignment
                    .Include(da => da.DeviceMessageCombo)
                        .ThenInclude(dmc => dmc.Message)
                    .Include(da => da.DeviceMessageCombo)
                        .ThenInclude(dmc => dmc.Device)
                    .Where(da => da.AssignmentType == "MESSAGE" &&
                                da.IsActive &&
                                da.DeviceMessageCombo.Device.DeviceType.ToLower() == deviceType.Trim().ToLower() &&
                                (storeId == null || da.StoreId == storeId))
                    .OrderBy(da => da.DisplayOrder)
                    .Select(da => new
                    {
                        da.Id,
                        DeviceId = da.DeviceMessageCombo.DeviceId,
                        DeviceName = da.DeviceMessageCombo.Device.Name,
                        DeviceMac = da.DeviceMessageCombo.Device.MACAddress,
                        MessageId = da.DeviceMessageCombo.MessageId,
                        MessageTitle = da.DeviceMessageCombo.Message.Title,
                        ContentType = da.DeviceMessageCombo.Message.ContentType,
                        ContentData = da.DeviceMessageCombo.Message.ContentData,
                        FileUrl = da.DeviceMessageCombo.Message.FileUrl,
                        Duration = da.DeviceMessageCombo.Message.Duration,
                        da.LocationType,
                        da.LocationId,
                        da.DisplayOrder,
                        da.IsActive,
                        da.StoreId,
                        da.CreatedDate
                    })
                    .ToListAsync();

                if (!assignments.Any())
                {
                    return ApiResponse(true, "No assignments found", new { assignments = new List<object>() });
                }

                return ApiResponse(true, "Assignments retrieved successfully", new { assignments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving public assignments for device type: {DeviceType}", deviceType);
                return ApiResponse(false, "Failed to retrieve assignments", null, ex.Message, 500);
            }
        }

        /// <summary>
        /// Get assignments by device ID - PUBLIC (no auth)
        /// </summary>
        [HttpGet("device/{deviceId}")]
        public async Task<IActionResult> GetAssignmentsByDeviceId(int deviceId, [FromQuery] long? storeId = null)
        {
            try
            {
                if (deviceId <= 0)
                {
                    return ApiResponse(false, "Valid device ID is required", null, "Invalid device ID", 400);
                }

                var assignments = await _context.DeviceAssignment
                    .Include(da => da.DeviceMessageCombo)
                        .ThenInclude(dmc => dmc.Message)
                    .Include(da => da.DeviceMessageCombo)
                        .ThenInclude(dmc => dmc.Device)
                    .Where(da =>
                        da.AssignmentType == "MESSAGE" &&
                        da.IsActive &&
                        da.DeviceMessageCombo.DeviceId == deviceId &&
                        (storeId == null || da.StoreId == storeId))
                    .OrderBy(da => da.DisplayOrder)
                    .Select(da => new
                    {
                        da.Id,
                        DeviceId = da.DeviceMessageCombo.DeviceId,
                        DeviceName = da.DeviceMessageCombo.Device.Name,
                        DeviceMac = da.DeviceMessageCombo.Device.MACAddress,
                        DeviceScreenWidth = da.DeviceMessageCombo.Device.DeviceScreen.Width,
                        DeviceScreenHeight = da.DeviceMessageCombo.Device.DeviceScreen.Height,
                        DeviceOrientation = da.DeviceMessageCombo.Device.DeviceScreen.AspectRatio,
                        MessageId = da.DeviceMessageCombo.MessageId,
                        MessageTitle = da.DeviceMessageCombo.Message.Title,
                        ContentType = da.DeviceMessageCombo.Message.ContentType,
                        ContentData = da.DeviceMessageCombo.Message.ContentData,
                        FileUrl = da.DeviceMessageCombo.Message.FileUrl,
                        Duration = da.DeviceMessageCombo.Message.Duration,
                        da.LocationType,
                        da.LocationId,
                        da.DisplayOrder,
                        da.IsActive,
                        da.StoreId,
                        da.CreatedDate
                    })
                    .ToListAsync();

                if (!assignments.Any())
                {
                    return ApiResponse(true, "No assignments found for this device", new { assignments = new List<object>() });
                }

                return ApiResponse(true, "Assignments retrieved successfully", new { assignments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assignments by device id: {DeviceId}", deviceId);
                return ApiResponse(false, "Failed to retrieve assignments", null, ex.Message, 500);
            }
        }

        /// <summary>
        /// Get Devices by store ID - PUBLIC (no auth)
        /// </summary>
        [HttpGet("devices/store/{storeId}")]
        public async Task<IActionResult> GetDevicesByStoreId(long storeId, string? deviceType)
        {
            try
            {
                if (storeId <= 0)
                {
                    return ApiResponse(false, "Valid store ID is required", null, "Invalid store ID", 400);
                }

                var query = _context.DeviceMaster
                .Include(d => d.DeviceScreen)
                .Where(d => d.StoreId == storeId && d.IsActive);

                // Apply device type filter if provided
                if (!string.IsNullOrEmpty(deviceType))
                {
                    query = query.Where(d => d.DeviceType.Trim().ToLower() == deviceType.Trim().ToLower());
                }

                var devices = await query
                    .OrderByDescending(d => d.CreatedDate)
                    .Select(d => new
                    {
                        Id = d.Id,
                        DeviceName = d.Name,
                        DeviceMac = d.MACAddress,
                        IPAddress = d.IPAddress,
                        NetworkName = d.NetworkName,
                        DeviceType = d.DeviceType,
                        IsActive = d.IsActive,
                        CreatedDate = d.CreatedDate
                    })
                    .ToListAsync();

                if (!devices.Any())
                {
                    return ApiResponse(true, "No devices found for this store", new { devices = new List<object>() });
                }

                return ApiResponse(true, "Devices retrieved successfully", new { devices });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving devices by store id: {StoreId}", storeId);
                return ApiResponse(false, "Failed to retrieve devices", null, ex.Message, 500);
            }
        }

        /// <summary>
        /// Get available messages - PUBLIC (no auth)
        /// </summary>
        [HttpGet("messages")]
        public async Task<IActionResult> GetMessages([FromQuery] bool activeOnly = true)
        {
            try
            {
                var query = _context.MessageMaster.AsQueryable();

                if (activeOnly)
                    query = query.Where(m => m.IsActive);

                var messages = await query
                    .OrderByDescending(m => m.CreatedDate)
                    .Select(m => new
                    {
                        m.Id,
                        m.Title,
                        m.ContentType,
                        ContentPreview = m.ContentData != null && m.ContentData.Length > 100
                            ? m.ContentData.Substring(0, 100) + "..."
                            : m.ContentData,
                        m.FileUrl,
                        m.Duration,
                        m.IsActive,
                        m.CreatedDate
                    })
                    .ToListAsync();

                if (!messages.Any())
                {
                    return ApiResponse(true, "No messages found", new { messages = new List<object>() });
                }

                return ApiResponse(true, "Messages retrieved successfully", new { messages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving messages");
                return ApiResponse(false, "Failed to retrieve messages", null, ex.Message, 500);
            }
        }

        /// <summary>
        /// Get device types - PUBLIC (no auth)
        /// </summary>
        [HttpGet("device-types")]
        public async Task<IActionResult> GetDeviceTypes()
        {
            try
            {
                var deviceTypes = await _context.DeviceMaster
                    .Where(d => d.IsActive)
                    .Select(d => d.DeviceType)
                    .Distinct()
                    .ToListAsync();

                if (!deviceTypes.Any())
                {
                    return ApiResponse(true, "No device types found", new { deviceTypes = new List<string>() });
                }

                return ApiResponse(true, "Device types retrieved successfully", new { deviceTypes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving device types");
                return ApiResponse(false, "Failed to retrieve device types", null, ex.Message, 500);
            }
        }

        /// <summary>
        /// Get stores - PUBLIC (no auth)
        /// </summary>
        [HttpGet("stores")]
        public async Task<IActionResult> GetStores([FromQuery] bool activeOnly = true)
        {
            try
            {
                var query = _context.StoreMaster.AsQueryable();

                if (activeOnly)
                    query = query.Where(s => s.IsActive);

                var stores = await query
                    .OrderBy(s => s.StoreName)
                    .Select(s => new
                    {
                        s.Id,
                        s.StoreName,
                        s.StoreCode,
                        s.StoreType,
                        s.IsActive,
                        s.Address
                    })
                    .ToListAsync();

                if (!stores.Any())
                {
                    return ApiResponse(true, "No stores found", new { stores = new List<object>() });
                }

                return ApiResponse(true, "Stores retrieved successfully", new { stores });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stores");
                return ApiResponse(false, "Failed to retrieve stores", null, ex.Message, 500);
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return ApiResponse(true, "API is running", new
            {
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                status = "healthy"
            });
        }
    }
}
