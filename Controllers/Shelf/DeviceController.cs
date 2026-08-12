using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Controllers;
using TERMS_LOYALTY_API.Data;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Interface;
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Models.shelf;
using TERMS_LOYALTY_API.Repository;
using TERMS_LOYALTY_API.Services;
using TERMS_LOYALTY_API.Services.Providers;
using TERMS_LOYALTY_API.Shared.Enum;
using TERMS_MOBILE_WEB_API.Models;
using static TERMS_LOYALTY_API.DTOs.shelf.AssignmentDto;
using static TERMS_LOYALTY_API.DTOs.shelf.ComboDto;
using static TERMS_LOYALTY_API.DTOs.shelf.DataBindDto;
using static TERMS_LOYALTY_API.DTOs.shelf.MinewDeviceBatchAdd;
using static TERMS_LOYALTY_API.DTOs.shelf.TemplateDto;

[Route("api/device")]
[ApiController]
[Authorize]
public class DeviceController : ControllerBase
{
    private readonly SmartShelfDbContext _context;
    private readonly MinewCloudService _minewService;
    private readonly IEslProviderFactory _eslProviderFactory;
    private readonly IDevice _deviceRepo;
    private readonly ILogger<DeviceController> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;


    public DeviceController(SmartShelfDbContext context, MinewCloudService minewService, IEslProviderFactory eslProviderFactory, IDevice device, ILogger<DeviceController> logger, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _minewService = minewService;
        _eslProviderFactory = eslProviderFactory;
        _deviceRepo = device;
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;
    }

    #region Helper Methods for .NET 5.0 JSON Handling
    private static string GetStringOrNull(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement property))
        {
            return property.ValueKind != JsonValueKind.Null ? property.GetString() : null;
        }
        return null;
    }

    private static int? GetInt32OrNull(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement property))
        {
            return property.ValueKind != JsonValueKind.Null ? property.GetInt32() : (int?)null;
        }
        return null;
    }

    private static decimal? GetDecimalOrNull(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement property))
        {
            return property.ValueKind != JsonValueKind.Null ? property.GetDecimal() : (decimal?)null;
        }
        return null;
    }

    private static DateTime? GetDateTimeOrNull(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement property))
        {
            if (property.ValueKind != JsonValueKind.Null &&
                DateTime.TryParse(property.GetString(), out DateTime date))
            {
                return date;
            }
        }
        return null;
    }

    private static Dictionary<string, string> GetGoodsMapFromProduct(ProductMaster product)
    {
        return new Dictionary<string, string>
        {
            ["id"] = product.Id.ToString(),
            ["price"] = product.SellingPrice.ToString("0.00"),
            ["name"] = product.ProductName,
            ["barcode"] = product.BarCode ?? "",
            ["p_code"] = product.ProductCode ?? ""
        };
    }
    #endregion

    // ============ STORE MANAGEMENT ============

    [HttpGet("stores")]
    public async Task<IActionResult> GetStores()
    {
        try
        {
            var token = await GetToken();

            var result = await _minewService.GetStoresAsync(token);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #region Device Handlers

    [HttpGet("devices/sync")]
    public async Task<IActionResult> SyncDevices([FromQuery] int storeId, [FromQuery] string eqstatus = "1,2,8,9")
    {
        try
        {
            // First, get the StoreMaster record to find the MinewStoreId
            var storeMaster = await _context.StoreMaster
                .FirstOrDefaultAsync(s => s.Id == storeId);

            if (storeMaster == null)
                return BadRequest(new { message = "Store not found" });

            var provider = _eslProviderFactory.GetProvider("Minew");
            var response = await provider.GetDevicesFromCloudAsync(storeMaster.MinewStoreId, eqstatus);

            if (response == null || response.Code != 200)
                return BadRequest(new { message = response?.Msg ?? "Cloud error" });

            if (response.Items == null || response.Items.Count == 0)
                return Ok(new { message = "No devices found", devices = new List<object>() });

            foreach (var device in response.Items)
            {
                if (string.IsNullOrEmpty(device.Id))
                    continue;

                // Look for existing device by MinewDeviceId
                var existing = await _context.DeviceMaster
                    .FirstOrDefaultAsync(d => d.MinewDeviceId == device.Id);

                DateTime.TryParse(device.Lastupdate, out var lastSeen);

                // Handle DeviceScreen lookup or creation
                long? screenId = null;
                if (device.ScreenInfo != null)
                {
                    // Find existing DeviceScreen record with matching dimensions and ESL
                    var deviceScreen = await _context.DeviceScreens
                        .FirstOrDefaultAsync(ds => ds.Width == device.ScreenInfo.Width &&
                                                   ds.Height == device.ScreenInfo.Height &&
                                                   ds.Inch == device.ScreenInfo.Inch &&
                                                   ds.ScreenTypeId == (int)ScreenType.ESL_Ink);

                    if (deviceScreen == null)
                    {
                        // Create new DeviceScreen record
                        deviceScreen = new DeviceScreen
                        {
                            Name = "Minew "+ device.ScreenSize,
                            Width = device.ScreenInfo.Width ?? 0,
                            Height = device.ScreenInfo.Height ?? 0,
                            Inch = device.ScreenInfo.Inch ?? 0,
                            ScreenTypeId = (int)ScreenType.ESL_Ink,
                            CreatedDate = DateTime.UtcNow,
                            CreatedUser = 0 // Set appropriate user ID
                        };
                        _context.DeviceScreens.Add(deviceScreen);
                        await _context.SaveChangesAsync(); // Save to get the ID
                    }

                    screenId = deviceScreen.Id;
                }


                if (existing == null)
                {
                    // Create new DeviceMaster
                    _context.DeviceMaster.Add(new DeviceMaster
                    {
                        MACAddress = device.Mac ?? "",
                        Name = device.remark ?? $"Minew Device {device.Mac.Substring(device.Mac.Length - 6)}",
                        Description = $"Minew Device: {device.ScreenSize}",
                        StatusId = device.IsOnline == "2" ? (int)DeviceStatus.Active : (int)DeviceStatus.Inactive, // Assuming 1=Online, 2=Offline in Status table
                        DeviceType = "Minew",
                        StoreId = storeMaster.Id, // Use StoreMaster Id, not MinewStoreId
                        MinewDeviceId = device.Id,
                        ScreenId = screenId,
                        ScreenColor = device.ScreenInfo?.Color,
                        Firmware = device.Firmware,
                        Hardware = device.Hardware,
                        Battery = device.Battery,
                        LastSeen = lastSeen == default ? DateTime.UtcNow : lastSeen,
                        LastSyncTime = DateTime.UtcNow,
                        IsActive = true,
                        IsOnline = device.IsOnline == "2",

                        // BaseEntity fields
                        CreatedDate = DateTime.UtcNow,
                        CreatedUser = 0, // Set appropriate user ID

                        // Optional fields that might be set later
                        NetworkName = device.ScreenSize ?? "Unknown",
                        IPAddress = "", // You might need to get this from another source
                    });
                }
                else
                {
                    // Update existing device
                    existing.StatusId = device.IsOnline == "2" ? (int)DeviceStatus.Active : (int)DeviceStatus.Inactive;
                    existing.Battery = device.Battery;
                    existing.IsOnline = device.IsOnline == "2";

                    if (lastSeen != default)
                        existing.LastSeen = lastSeen;

                    existing.LastSyncTime = DateTime.UtcNow;
                    existing.UpdatedDate = DateTime.UtcNow;
                    existing.UpdatedUser = 0; // Set appropriate user ID

                    // Update screen info if available
                    if (device.ScreenInfo != null)
                    {
                        existing.ScreenId = screenId;
                        existing.ScreenColor = device.ScreenInfo.Color;
                    }

                    // Update firmware/hardware if changed
                    if (!string.IsNullOrEmpty(device.Firmware))
                        existing.Firmware = device.Firmware;

                    if (!string.IsNullOrEmpty(device.Hardware))
                        existing.Hardware = device.Hardware;
                }
            }

            await _context.SaveChangesAsync();

            // Get updated devices for response
            var devices = await _context.DeviceMaster
                .Where(d => d.StoreId == storeMaster.Id && d.IsActive)
                .Include(d => d.DeviceScreen)
                .Select(d => new
                {
                    d.Id,
                    mac = d.MACAddress,
                    deviceName = d.Name,
                    ScreenInch = d.DeviceScreen.Inch,
                    ScreenWidth = d.DeviceScreen.Width,
                    ScreenHeight = d.DeviceScreen.Height,
                    d.ScreenColor,
                    d.Firmware,
                    d.Hardware,
                    status = d.Status.Name, // Assuming Status table has StatusName field
                    d.Battery,
                    d.LastSeen,
                    storeId = d.StoreId,
                    isOnline = d.IsOnline,
                    minewDeviceId = d.MinewDeviceId,
                    d.DeviceType
                })
                .ToListAsync();

            // Update store sync information
            storeMaster.LastSyncDate = DateTime.UtcNow;
            storeMaster.SyncStatus = "success";
            storeMaster.IsSynced = true;
            await _context.SaveChangesAsync();

            return Ok(devices);
        }
        catch (Exception ex)
        {
            // Update store sync status to failed
            var storeMaster = await _context.StoreMaster
                .FirstOrDefaultAsync(s => s.Id == storeId);

            if (storeMaster != null)
            {
                storeMaster.LastSyncDate = DateTime.UtcNow;
                storeMaster.SyncStatus = "failed";
                await _context.SaveChangesAsync();
            }

            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("devices/local")]
    public async Task<IActionResult> GetLocalDevices()
    {
        try
        {
            var devices = await _context.DeviceMaster
                .Include(d => d.DeviceScreen)
                .Where(d => d.IsActive)
                .Select(d => new
                {
                    id = d.Id,
                    mac = d.MACAddress,
                    deviceName = d.Name,
                    screenSize = d.DeviceScreen.AspectRatio,
                    ScreenInch = d.DeviceScreen.Inch,
                    ScreenHeight = d.DeviceScreen.Height,
                    ScreenWidth = d.DeviceScreen.Width,
                    ScreenColor = d.ScreenColor,
                    status = d.Status,
                    battery = d.Battery,
                    lastSeen = d.LastSeen,
                    storeId = d.StoreId,
                    isOnline = d.StatusId == 0,
                })
                .ToListAsync();

            return Ok(devices);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lists active ESL brands (e.g. Minew, Standard) for device/ESL-assignment brand pickers.
    /// </summary>
    [HttpGet("brands")]
    [ProducesResponseType(typeof(HttpResponseData<List<EslBrandDto>>), 200)]
    public async Task<IActionResult> GetBrands()
    {
        try
        {
            var brands = await _deviceRepo.GetActiveBrandsAsync();
            return Ok(new HttpResponseData<List<EslBrandDto>>
            {
                Success = true,
                Result = brands
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ESL brands");
            return StatusCode(500, new HttpResponseData<List<EslBrandDto>>
            {
                Success = false,
                Message = "Failed to fetch ESL brands",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Retrieves a device by ID.
    /// </summary>
    [HttpGet("device/{id:int}")]
    [ProducesResponseType(typeof(HttpResponseData<DeviceDto>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<DeviceDto>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<DeviceDto>), 500)]
    public async Task<IActionResult> GetDeviceById(long id)
    {
        var response = new HttpResponseData<DeviceDto>();
        try
        {
            var device = await _deviceRepo.GetDeviceByIdAsync(id);
            if (device == null)
            {
                response.Success = false;
                response.Message = $"Device with ID {id} not found.";
                response.ResponsCode = 404;
                return NotFound(response);
            }

            response.Success = true;
            response.Message = "Device retrieved successfully.";
            response.Result = device;
            response.ResponsCode = 200;
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving device ID {id}");
            response.Success = false;
            response.Message = "Failed to retrieve device.";
            response.Error = ex.Message;
            response.ResponsCode = 500;
            return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Retrieves devices by a list of IDs.
    /// </summary>
    [HttpPost("devices/by-ids")]
    [ProducesResponseType(typeof(HttpResponseData<List<DeviceDto>>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<List<DeviceDto>>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<List<DeviceDto>>), 500)]
    public async Task<IActionResult> GetTemplatesByIds([FromBody] List<long> ids)
    {
        var response = new HttpResponseData<List<DeviceDto>>();

        try
        {
            if (ids == null || !ids.Any())
            {
                response.Success = false;
                response.Message = "Template ID list is empty.";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            var devices = await _deviceRepo.GetDevicesByIdsAsync(ids);

            if (devices == null || !devices.Any())
            {
                response.Success = false;
                response.Message = "No devices found for the provided IDs.";
                response.ResponsCode = 404;
                return NotFound(response);
            }

            response.Success = true;
            response.Message = "Devices retrieved successfully.";
            response.Result = devices;
            response.ResponsCode = 200;

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving templates by IDs");

            response.Success = false;
            response.Message = "Failed to retrieve templates.";
            response.Error = ex.Message;
            response.ResponsCode = 500;

            return Problem(
                title: response.Message,
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    [HttpPost("device")]
    [ProducesResponseType(typeof(HttpResponseData<DeviceDto>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 409)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> CreateDevice([FromBody] CreateDeviceRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Error = ModelState.Values.ToString()
                });
            }

            var result = await _deviceRepo.CreateDeviceAsync(request);

            return Ok(new HttpResponseData<DeviceDto>
            {
                Success = true,
                Message = "Device created successfully",
                Result = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating device");
            return BadRequest(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation creating device");
            return Conflict(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating device");
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while creating the device",
                Error = ex.Message
            });
        }
    }

    [HttpPut("device/{id}")]
    [ProducesResponseType(typeof(HttpResponseData<DeviceDto>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 409)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> UpdateDevice(long id, [FromBody] UpdateDeviceRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Error = ModelState.Values.ToString()
                });
            }

            request.Id = id; // Ensure ID from route is used
            var result = await _deviceRepo.UpdateDeviceAsync(id, request);

            return Ok(new HttpResponseData<DeviceDto>
            {
                Success = true,
                Message = "Device updated successfully",
                Result = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Device not found for update");
            return NotFound(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating device");
            return BadRequest(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation updating device");
            return Conflict(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while updating the device",
                Error = ex.Message
            });
        }
    }

    [HttpDelete("device/{id}/user/{userId}")]
    [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> DeleteDevice(long id, int userId)
    {
        try
        {
            var (success, message) = await _deviceRepo.DeleteDeviceAsync(id, userId);

            if (!success)
            {
                // Cannot delete due to active combos
                return Ok(new HttpResponseData<bool>
                {
                    Success = false,
                    Message = message,
                    Result = false
                });
            }

            return Ok(new HttpResponseData<bool>
            {
                Success = true,
                Message = message,
                Result = true
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Device not found for deletion");
            return NotFound(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting device ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while deleting the device",
                Error = ex.Message
            });
        }
    }


    [HttpGet("devices/light-up")]
    public async Task<IActionResult> LightUpDevice([FromQuery] string mac, [FromQuery] string storeId,
    [FromQuery] int color = 1, [FromQuery] int total = 5, [FromQuery] int period = 500,
    [FromQuery] int interval = 900, [FromQuery] int brightness = 100)
    {
        try
        {
            var device = await _context.DeviceMaster.FirstOrDefaultAsync(d => d.MACAddress == mac);
            var provider = _eslProviderFactory.GetProvider(device?.DeviceType ?? "Minew");
            var result = await provider.LightUpDeviceAsync(mac, storeId, color, total, period, interval, brightness);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("devices/batch-add-minew")]
    [ProducesResponseType(typeof(HttpResponseData<BatchAddResult>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<BatchAddResult>), 400)]
    [ProducesResponseType(typeof(HttpResponseData<BatchAddResult>), 500)]
    public async Task<IActionResult> BatchAddDevicesToMinew([FromBody] BatchAddDeviceRequest request)
    {
        var response = new HttpResponseData<BatchAddResult>();

        try
        {
            if (request == null)
            {
                response.Success = false;
                response.Message = "Request is required";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.StoreId))
            {
                response.Success = false;
                response.Message = "Store ID is required";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            if (request.MacAddresses == null || !request.MacAddresses.Any())
            {
                response.Success = false;
                response.Message = "At least one MAC address is required";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            // Clean MAC addresses (remove colons, dashes, spaces and convert to lowercase)
            var cleanedMacs = request.MacAddresses
                .Where(mac => !string.IsNullOrWhiteSpace(mac))
                .Select(mac => mac.Replace(":", "").Replace("-", "").Replace(" ", "").ToLower())
                .Distinct()
                .ToList();

            if (!cleanedMacs.Any())
            {
                response.Success = false;
                response.Message = "No valid MAC addresses provided";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            // Validate MAC address format (12 hex characters)
            var invalidMacs = cleanedMacs
                .Where(mac => mac.Length != 12 || !Regex.IsMatch(mac, @"^[0-9a-f]{12}$"))
                .ToList();

            if (invalidMacs.Any())
            {
                response.Success = false;
                response.Message = $"Invalid MAC address format: {string.Join(", ", invalidMacs.Take(5))}";
                response.ResponsCode = 400;
                return BadRequest(response);
            }
            var token = await GetToken();

            // Call Minew API to batch add devices
            var minewResponse = await _minewService.BatchAddDevicesAsync(
                token,
                request.StoreId,
                cleanedMacs,
                request.Type);

            if (minewResponse == null)
            {
                response.Success = false;
                response.Message = "Failed to add devices to Minew";
                response.ResponsCode = 500;
                return StatusCode(500, response);
            }

            // Analyze results
            var addedCount = minewResponse.Data?.Values.Count(v => v.ToLower() == "success") ?? 0;
            var failedCount = cleanedMacs.Count - addedCount;

            // STEP 1: Wake up the successfully added devices
            var wakeResult = new BatchWakeResult();
            List<string> successfullyWokenMacs = new List<string>();
            List<string> failedToWakeMacs = new List<string>();

            if (minewResponse.Code == 200 && addedCount > 0)
            {
                try
                {
                    // Get only the successfully added MACs
                    var successMacs = minewResponse.Data
                        .Where(kv => kv.Value.ToLower() == "success")
                        .Select(kv => kv.Key)
                        .ToList();

                    if (successMacs.Any())
                    {
                        // Attempt to wake up devices
                        var wakeResponse = await _minewService.BatchWakeDevicesAsync(
                            token,
                            request.StoreId,
                            successMacs);

                        wakeResult.Success = wakeResponse.Code == 200;
                        wakeResult.Message = wakeResponse.Msg;

                        if (wakeResponse.Code == 200)
                        {
                            successfullyWokenMacs = successMacs;
                            wakeResult.WokeCount = successMacs.Count;

                            // Log success
                            _logger.LogInformation($"Successfully woke up {successMacs.Count} devices");
                        }
                        else
                        {
                            // All devices failed to wake up
                            failedToWakeMacs = successMacs;
                            wakeResult.FailedCount = successMacs.Count;
                            wakeResult.Message = $"Wake up failed: {wakeResponse.Msg}";

                            _logger.LogWarning($"Wake up failed for {successMacs.Count} devices: {wakeResponse.Msg}");
                        }
                    }
                }
                catch (Exception wakeEx)
                {
                    // All devices failed to wake up due to exception
                    failedToWakeMacs = cleanedMacs;
                    wakeResult.Success = false;
                    wakeResult.Message = wakeEx.Message;
                    wakeResult.FailedCount = cleanedMacs.Count;

                    _logger.LogWarning(wakeEx, "Failed to wake up devices");
                }
            }

            var result = new BatchAddResult
            {
                Success = minewResponse.Code == 200,
                Message = minewResponse.Msg,
                AddedCount = addedCount,
                FailedCount = failedCount,
                Results = minewResponse.Data ?? new Dictionary<string, string>(),
                WakeUpResult = wakeResult,
                WokenDevices = successfullyWokenMacs,
                FailedToWakeDevices = failedToWakeMacs
            };

            // STEP 2: If successful, add to local database
            if (minewResponse.Code == 200 && addedCount > 0)
            {
                // Find store in local database
                var store = await _context.StoreMaster
                    .FirstOrDefaultAsync(s => s.MinewStoreId == request.StoreId);

                if (store != null)
                {
                    foreach (var mac in cleanedMacs)
                    {
                        // Check if device already exists in local database
                        var existingDevice = await _context.DeviceMaster
                            .FirstOrDefaultAsync(d => d.MACAddress == mac && d.StoreId == store.Id);

                        if (existingDevice == null)
                        {
                            // Determine status based on wake up result
                            int statusId;
                            if (successfullyWokenMacs.Contains(mac))
                            {
                                // Successfully woken - set as offline initially (status 2)
                                statusId = (int)DeviceStatus.Offline;
                            }
                            else if (failedToWakeMacs.Contains(mac))
                            {
                                // Failed to wake - set as maintenance (status 4)
                                statusId = (int)DeviceStatus.Maintenance;
                            }
                            else
                            {
                                // Not attempted to wake (shouldn't happen) - set as offline
                                statusId = (int)DeviceStatus.Offline;
                            }

                            // Create new device record in local database
                            var newDevice = new DeviceMaster
                            {
                                MACAddress = mac,
                                Name = $"Minew Device {mac.Substring(mac.Length - 6)}",
                                Description = "Added via batch import",
                                StatusId = statusId, // Set based on wake status
                                DeviceType = "Minew",
                                StoreId = store.Id,
                                ScreenId = null,
                                MinewDeviceId = mac, // Use MAC as temporary ID until sync
                                IsActive = true,
                                IsOnline = false,
                                Battery = 0,
                                LastSeen = DateTime.UtcNow,
                                LastSyncTime = DateTime.UtcNow,
                                CreatedDate = DateTime.UtcNow,
                                CreatedUser = request.UserId
                            };

                            _context.DeviceMaster.Add(newDevice);
                        }
                        else
                        {
                            // Update existing device - update status if wake failed
                            if (failedToWakeMacs.Contains(mac) && existingDevice.StatusId != (int)DeviceStatus.Maintenance)
                            {
                                existingDevice.StatusId = (int)DeviceStatus.Maintenance; // Maintenance
                                existingDevice.UpdatedDate = DateTime.UtcNow;
                                existingDevice.UpdatedUser = request.UserId;
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                }
            }

            // STEP 3: Return response with wake info
            string finalMessage = minewResponse.Code == 200
                ? $"Devices added successfully. {addedCount} devices added." +
                  (wakeResult.WokeCount > 0 ? $" {wakeResult.WokeCount} devices woken up." : "") +
                  (wakeResult.FailedCount > 0 ? $" {wakeResult.FailedCount} devices failed to wake up (set to maintenance)." : "")
                : minewResponse.Msg;

            response.Success = minewResponse.Code == 200;
            response.Message = finalMessage;
            response.Result = result;
            response.ResponsCode = minewResponse.Code == 200 ? 200 : 400;

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch adding devices to Minew");

            response.Success = false;
            response.Message = $"Failed to add devices: {ex.Message}";
            response.ResponsCode = 500;

            return StatusCode(500, response);
        }
    }

    [HttpPost("devices/batch-add-minew-upload")]
    [ProducesResponseType(typeof(HttpResponseData<BatchAddResult>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<BatchAddResult>), 400)]
    [ProducesResponseType(typeof(HttpResponseData<BatchAddResult>), 500)]
    public async Task<IActionResult> BatchAddDevicesFromExcel([FromForm] IFormFile file, [FromQuery] string storeId, [FromQuery] int type = 1, [FromQuery] int userId = 0)
    {
        var response = new HttpResponseData<BatchAddResult>();

        try
        {
            if (file == null || file.Length == 0)
            {
                response.Success = false;
                response.Message = "Please upload a file";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(storeId))
            {
                response.Success = false;
                response.Message = "Store ID is required";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            // Validate file type
            var allowedExtensions = new[] { ".xlsx", ".xls", ".csv", ".txt" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                response.Success = false;
                response.Message = "Invalid file type. Please upload Excel (.xlsx, .xls) or CSV/TXT files";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            List<string> macAddresses = new List<string>();

            // Read file based on type
            if (extension == ".csv" || extension == ".txt")
            {
                using var reader = new StreamReader(file.OpenReadStream());
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        macAddresses.Add(line.Trim());
                    }
                }
            }
            else // Excel file
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.First();

                foreach (var row in worksheet.RowsUsed())
                {
                    var cellValue = row.Cell(1).GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(cellValue))
                    {
                        macAddresses.Add(cellValue);
                    }
                }
            }

            if (!macAddresses.Any())
            {
                response.Success = false;
                response.Message = "No MAC addresses found in the file";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            // Call the existing batch add method
            var batchRequest = new BatchAddDeviceRequest
            {
                StoreId = storeId,
                MacAddresses = macAddresses,
                Type = type,
                UserId = userId
            };

            return await BatchAddDevicesToMinew(batchRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing uploaded file");

            response.Success = false;
            response.Message = $"Failed to process file: {ex.Message}";
            response.ResponsCode = 500;

            return StatusCode(500, response);
        }
    }

    [HttpPost("devices/batch-wake")]
    [ProducesResponseType(typeof(HttpResponseData<BatchWakeResult>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<BatchWakeResult>), 400)]
    [ProducesResponseType(typeof(HttpResponseData<BatchWakeResult>), 500)]
    public async Task<IActionResult> BatchWakeDevices([FromBody] BatchWakeDevicesRequest request)
    {
        var response = new HttpResponseData<BatchWakeResult>();

        try
        {
            if (request == null)
            {
                response.Success = false;
                response.Message = "Request is required";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.StoreId))
            {
                response.Success = false;
                response.Message = "Store ID is required";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            if (request.MacAddresses == null || !request.MacAddresses.Any())
            {
                response.Success = false;
                response.Message = "At least one MAC address is required";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            // Clean MAC addresses
            var cleanedMacs = request.MacAddresses
                .Where(mac => !string.IsNullOrWhiteSpace(mac))
                .Select(mac => mac.Replace(":", "").Replace("-", "").Replace(" ", "").ToLower())
                .Distinct()
                .ToList();

            if (!cleanedMacs.Any())
            {
                response.Success = false;
                response.Message = "No valid MAC addresses provided";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            var token = await GetToken();

            // Call Minew API to wake up devices
            var minewResponse = await _minewService.BatchWakeDevicesAsync(
                token,
                request.StoreId,
                cleanedMacs);

            var result = new BatchWakeResult
            {
                Success = minewResponse.Code == 200,
                Message = minewResponse.Msg,
                WokeCount = minewResponse.Code == 200 ? cleanedMacs.Count : 0,
                FailedCount = minewResponse.Code != 200 ? cleanedMacs.Count : 0
            };

            // Update local database with wake status - set status to maintenance (4) if wake failed
            var store = await _context.StoreMaster
                .FirstOrDefaultAsync(s => s.MinewStoreId == request.StoreId);

            if (store != null)
            {
                foreach (var mac in cleanedMacs)
                {
                    var device = await _context.DeviceMaster
                        .FirstOrDefaultAsync(d => d.MACAddress == mac && d.StoreId == store.Id);

                    if (device != null)
                    {
                        if (minewResponse.Code == 200)
                        {
                            // Wake successful - keep current status or set to offline (2) if currently maintenance
                            if (device.StatusId == (int)DeviceStatus.Maintenance) // If was in maintenance
                            {
                                device.StatusId = (int)DeviceStatus.Offline; // Set to offline
                            }
                            device.UpdatedDate = DateTime.UtcNow;
                            device.UpdatedUser = request.UserId;
                        }
                        else
                        {
                            // Wake failed - set to maintenance (4)
                            device.StatusId = (int)DeviceStatus.Maintenance; // Maintenance
                            device.UpdatedDate = DateTime.UtcNow;
                            device.UpdatedUser = request.UserId;
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }

            response.Success = minewResponse.Code == 200;
            response.Message = minewResponse.Code == 200
                ? $"Successfully woke up {cleanedMacs.Count} devices"
                : $"Failed to wake up devices. Devices set to maintenance status. Error: {minewResponse.Msg}";
            response.Result = result;
            response.ResponsCode = minewResponse.Code == 200 ? 200 : 400;

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error waking up devices");

            response.Success = false;
            response.Message = $"Failed to wake up devices: {ex.Message}";
            response.ResponsCode = 500;

            return StatusCode(500, response);
        }
    }

    [HttpPost("devices/delayed-sync")]
    [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> DelayedSyncAfterWake([FromQuery] string storeId, [FromQuery] int delaySeconds = 30)
    {
        var response = new HttpResponseData<object>();

        try
        {
            if (string.IsNullOrEmpty(storeId))
            {
                response.Success = false;
                response.Message = "Store ID is required";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            // Find store in local database
            var store = await _context.StoreMaster
                .FirstOrDefaultAsync(s => s.MinewStoreId == storeId);

            if (store == null)
            {
                response.Success = false;
                response.Message = "Store not found in local database";
                response.ResponsCode = 404;
                return NotFound(response);
            }

            // Get devices in maintenance status (4) that were added recently
            var maintenanceDevices = await _context.DeviceMaster
                .Where(d => d.StoreId == store.Id &&
                           d.StatusId == (int)DeviceStatus.Maintenance && // Maintenance status
                           d.CreatedDate > DateTime.UtcNow.AddHours(-1)) // Added in last hour
                .Select(d => new { d.Id, d.MACAddress })
                .ToListAsync();

            // Wait for devices to wake up
            await Task.Delay(delaySeconds * 1000);

            // Sync from cloud
            var token = await GetToken();
            var syncResponse = await _minewService.GetDevicesFromCloud(token, storeId, "1,2,8,9");

            if (syncResponse?.Code == 200 && syncResponse.Items != null)
            {
                int syncedCount = 0;
                int maintenanceClearedCount = 0;

                foreach (var cloudDevice in syncResponse.Items)
                {
                    var localDevice = await _context.DeviceMaster
                        .FirstOrDefaultAsync(d => d.MACAddress == cloudDevice.Mac && d.StoreId == store.Id);

                    // Handle DeviceScreen lookup or creation
                    long? screenId = null;
                    if (cloudDevice.ScreenInfo != null)
                    {
                        // Find existing DeviceScreen record with matching dimensions and ESL
                        var deviceScreen = await _context.DeviceScreens
                            .FirstOrDefaultAsync(ds => ds.Width == cloudDevice.ScreenInfo.Width &&
                                                       ds.Height == cloudDevice.ScreenInfo.Height &&
                                                       ds.Inch == cloudDevice.ScreenInfo.Inch &&
                                                       ds.ScreenTypeId == (int)ScreenType.ESL_Ink);

                    }
                        if (localDevice != null)
                    {
                        // Update device with cloud data
                        localDevice.MinewDeviceId = cloudDevice.Id;
                        localDevice.Name = cloudDevice.ScreenSize ?? localDevice.Name;
                        localDevice.ScreenId = screenId;
                        localDevice.ScreenColor = cloudDevice.ScreenInfo?.Color;
                        localDevice.Firmware = cloudDevice.Firmware;
                        localDevice.Hardware = cloudDevice.Hardware;
                        localDevice.Battery = cloudDevice.Battery;
                        localDevice.IsOnline = cloudDevice.IsOnline == "2";
                        localDevice.StatusId = cloudDevice.IsOnline == "2" ? (int)DeviceStatus.Active : (int)DeviceStatus.Inactive; // Online(1) or Offline(2)

                        // Clear maintenance status if it was set
                        bool wasMaintenance = localDevice.StatusId == (int)DeviceStatus.Maintenance;
                        localDevice.StatusId = cloudDevice.IsOnline == "2" ? (int)DeviceStatus.Active : (int)DeviceStatus.Inactive;

                        localDevice.LastSeen = DateTime.TryParse(cloudDevice.Lastupdate, out var lastSeen)
                            ? lastSeen
                            : DateTime.UtcNow;
                        localDevice.LastSyncTime = DateTime.UtcNow;
                        localDevice.UpdatedDate = DateTime.UtcNow;

                        syncedCount++;
                        if (wasMaintenance) maintenanceClearedCount++;
                    }
                }

                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = $"Successfully synced {syncedCount} devices from cloud. " +
                                  $"{maintenanceClearedCount} devices cleared from maintenance status.";
                response.Result = new
                {
                    devicesSynced = syncedCount,
                    maintenanceCleared = maintenanceClearedCount,
                    totalMaintenanceDevices = maintenanceDevices.Count
                };
            }
            else
            {
                response.Success = false;
                response.Message = syncResponse?.Msg ?? "Failed to sync devices from cloud";
                response.Result = new
                {
                    devicesSynced = 0,
                    maintenanceCleared = 0,
                    totalMaintenanceDevices = maintenanceDevices.Count
                };
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in delayed sync");

            response.Success = false;
            response.Message = $"Failed to sync: {ex.Message}";
            response.ResponsCode = 500;

            return StatusCode(500, response);
        }
    }
    #endregion

    #region Device Screen 

    [HttpGet("screen/{id}")]
    public async Task<IActionResult> GetScreenById(long id)
    {
        try
        {
            var result = await _deviceRepo.GetScreenByIdAsync(id);
            return Ok(new HttpResponseData<DeviceScreenDto>
            {
                Success = true,
                Result = result,
                Message = "Screen retrieved successfully"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting screen by ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while retrieving the screen"
            });
        }
    }

    [HttpGet("screen/paged")]
    public async Task<IActionResult> GetAllScreenPaged([FromQuery] DeviceScreenPagedRequest request)
    {
        try
        {
            var result = await _deviceRepo.GetPagedAsync(request);
            return Ok(new HttpResponseData<PagedResult<DeviceScreenDto>>
            {
                Success = true,
                Result = result,
                Message = "Screens retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paginated screens");
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while retrieving screens"
            });
        }
    }

    [HttpGet("screen/available")]
    public async Task<IActionResult> GetAvailableScreens([FromQuery] long? screenTypeId = null)
    {
        try
        {
            var result = await _deviceRepo.GetAvailableScreensAsync(screenTypeId);
            return Ok(new HttpResponseData<List<DeviceScreenDto>>
            {
                Success = true,
                Result = result,
                Message = "Available screens retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available screens");
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while retrieving available screens"
            });
        }
    }

    [HttpPost("screen")]
    public async Task<IActionResult> CreateScreen([FromBody] DeviceScreenCreateDto createDto)
    {
        try
        {
            var result = await _deviceRepo.CreateScreenAsync(createDto);
            return Ok(new HttpResponseData<DeviceScreenDto>
            {
                Success = true,
                Result = result,
                Message = "Screen created successfully"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating screen");
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while creating the screen"
            });
        }
    }

    [HttpPut("screen")]
    public async Task<IActionResult> UpdateScreen([FromBody] DeviceScreenUpdateDto updateDto)
    {
        try
        {
            var result = await _deviceRepo.UpdateScreenAsync(updateDto);
            return Ok(new HttpResponseData<DeviceScreenDto>
            {
                Success = true,
                Result = result,
                Message = "Screen updated successfully"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating screen with ID: {Id}", updateDto.Id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while updating the screen"
            });
        }
    }

    [HttpDelete("screen/{id}")]
    public async Task<IActionResult> DeleteScreen(long id)
    {
        try
        {
            var canDelete = await _deviceRepo.CanDeleteScreenAsync(id);
            if (!canDelete)
            {
                var deviceCount = await _deviceRepo.GetDeviceCountByScreenIdAsync(id);
                return BadRequest(new HttpResponseData<object>
                {
                    Success = false,
                    Message = $"Cannot delete screen. It is currently used by {deviceCount} device(s)."
                });
            }

            var result = await _deviceRepo.DeleteScreenAsync(id);
            return Ok(new HttpResponseData<bool>
            {
                Success = true,
                Result = result,
                Message = "Screen deleted successfully"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting screen with ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while deleting the screen"
            });
        }
    }

    [HttpPatch("screen/{id}/toggle-active")]
    public async Task<IActionResult> ToggleScreenActive(long id, [FromBody] int userId)
    {
        try
        {
            var result = await _deviceRepo.ToggleScreenActiveAsync(id, userId);
            return Ok(new HttpResponseData<bool>
            {
                Success = true,
                Result = result,
                Message = "Screen active status toggled successfully"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling active status for screen with ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while toggling active status"
            });
        }
    }

    [HttpGet("screen/{id}/device-count")]
    public async Task<IActionResult> GetScreenDeviceCount(long id)
    {
        try
        {
            var result = await _deviceRepo.GetDeviceCountByScreenIdAsync(id);
            return Ok(new HttpResponseData<int>
            {
                Success = true,
                Result = result,
                Message = "Device count retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device count for screen with ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while getting device count"
            });
        }
    }

    [HttpGet("screen/{id}/can-delete")]
    public async Task<IActionResult> CanScreenDelete(long id)
    {
        try
        {
            var result = await _deviceRepo.CanDeleteScreenAsync(id);
            return Ok(new HttpResponseData<bool>
            {
                Success = true,
                Result = result,
                Message = result ? "Screen can be deleted" : "Screen cannot be deleted (in use)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if screen can be deleted with ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while checking deletion status"
            });
        }
    }

    #endregion

    // Controllers/DeviceController.cs - Add these methods to existing controller
    #region Gateway Management

    [HttpGet("gateways/paged")]
    [ProducesResponseType(typeof(HttpResponseData<PagedResult<GatewayDto>>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> GetGatewaysPaged([FromQuery] GatewayPagedRequest request)
    {
        try
        {
            var result = await _deviceRepo.GetGatewaysPagedAsync(request);

            return Ok(new HttpResponseData<PagedResult<GatewayDto>>
            {
                Success = true,
                Message = "Gateways retrieved successfully",
                ResponsCode = 200,
                Result = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated gateways");
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "Error retrieving gateways",
                Error = ex.Message
            });
        }
    }

    [HttpGet("gateway/{id}")]
    [ProducesResponseType(typeof(HttpResponseData<GatewayDto>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> GetGatewayById(long id)
    {
        var response = new HttpResponseData<GatewayDto>();
        try
        {
            var gateway = await _deviceRepo.GetGatewayByIdAsync(id);

            response.Success = true;
            response.Message = "Gateway retrieved successfully";
            response.Result = gateway;
            response.ResponsCode = 200;
            return Ok(response);
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
            _logger.LogError(ex, $"Error retrieving gateway ID {id}");
            response.Success = false;
            response.Message = "Failed to retrieve gateway";
            response.Error = ex.Message;
            response.ResponsCode = 500;
            return StatusCode(500, response);
        }
    }

    [HttpPost("gateway")]
    [ProducesResponseType(typeof(HttpResponseData<GatewayDto>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 409)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> CreateGateway([FromBody] CreateGatewayRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Error = ModelState.Values.ToString()
                });
            }

            var result = await _deviceRepo.CreateGatewayAsync(request);

            return Ok(new HttpResponseData<GatewayDto>
            {
                Success = true,
                Message = "Gateway created successfully",
                Result = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating gateway");
            return BadRequest(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation creating gateway");
            return Conflict(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating gateway");
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while creating the gateway",
                Error = ex.Message
            });
        }
    }

    [HttpPut("gateway/{id}")]
    [ProducesResponseType(typeof(HttpResponseData<GatewayDto>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 409)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> UpdateGateway(long id, [FromBody] UpdateGatewayRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Error = ModelState.Values.ToString()
                });
            }

            request.Id = id;
            var result = await _deviceRepo.UpdateGatewayAsync(id, request);

            return Ok(new HttpResponseData<GatewayDto>
            {
                Success = true,
                Message = "Gateway updated successfully",
                Result = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Gateway not found for update");
            return NotFound(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating gateway ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while updating the gateway",
                Error = ex.Message
            });
        }
    }

    [HttpDelete("gateway/{id}/user/{userId}")]
    [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> DeleteGateway(long id, int userId)
    {
        try
        {
            var (success, message) = await _deviceRepo.DeleteGatewayAsync(id, userId);

            if (!success)
            {
                return Ok(new HttpResponseData<bool>
                {
                    Success = false,
                    Message = message,
                    Result = false
                });
            }

            return Ok(new HttpResponseData<bool>
            {
                Success = true,
                Message = message,
                Result = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting gateway ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while deleting the gateway",
                Error = ex.Message
            });
        }
    }

    [HttpGet("gateways/sync")]
    public async Task<IActionResult> SyncGateways([FromQuery] long storeId)
    {
        try
        {
            // First, get the StoreMaster record to find the MinewStoreId
            var storeMaster = await _context.StoreMaster
                .FirstOrDefaultAsync(s => s.Id == storeId);

            if (storeMaster == null)
                return BadRequest(new { message = "Store not found" });

            var token = await GetToken();
            await _deviceRepo.SyncGatewaysFromCloudAsync(token, storeMaster.MinewStoreId, storeId);

            // Get updated gateways for response
            var gateways = await _context.GatewayMaster
                .Where(g => g.StoreId == storeId && g.IsActive)
                .Include(g => g.Store)
                .Include(g => g.Status)
                .Select(g => new GatewayDto
                {
                    Id = g.Id,
                    MacAddress = g.MacAddress,
                    Name = g.Name,
                    Description = g.Description,
                    StoreName = g.Store.StoreName,
                    MinewGatewayId = g.MinewGatewayId,
                    Status = g.Status.Name,
                    GatewayType = g.GatewayType,
                    HardwareVersion = g.HardwareVersion,
                    FirmwareVersion = g.FirmwareVersion,
                    Battery = g.Battery,
                    IsOnline = g.IsOnline,
                    LastSeen = g.LastSeen,
                    LastSyncTime = g.LastSyncTime,
                    IsActive = g.IsActive
                })
                .ToListAsync();

            return Ok(new { message = "Gateways synced successfully", gateways });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("gateway/add-to-minew")]
    public async Task<IActionResult> AddGatewayToMinew([FromBody] AddGatewayToMinewRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Mac) ||
                string.IsNullOrEmpty(request.Name) ||
                string.IsNullOrEmpty(request.StoreId))
            {
                return BadRequest(new { message = "MAC, Name, and StoreId are required" });
            }

            var token = await GetToken();
            var result = await _deviceRepo.AddGatewayToMinewCloudAsync(token, request.Mac, request.Name, request.StoreId);

            // If successful, add to local database
            if (result != null && result.code == 200)
            {
                // Find store in local database
                var store = await _context.StoreMaster
                    .FirstOrDefaultAsync(s => s.MinewStoreId == request.StoreId);

                if (store != null)
                {
                    var gateway = new GatewayMaster
                    {
                        MacAddress = request.Mac.Replace(":", "").Replace("-", "").Replace(" ", "").ToLower(),
                        Name = request.Name,
                        Description = "Added to Minew Cloud",
                        StoreId = store.Id,
                        GatewayType = "Minew",
                        StatusId = 1, // Active
                        IsOnline = false,
                        LastSeen = DateTime.UtcNow,
                        LastSyncTime = DateTime.UtcNow,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        CreatedUser = request.UserId
                    };

                    _context.GatewayMaster.Add(gateway);
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    #endregion

    #region Minew Template Handlers

    [HttpGet("template/local")]
    public async Task<IActionResult> GetLocalTemplate()
    {
        try
        {
            var devices = await _context.MinewTemplates
                .Where(d => d.IsActive)
                .ToListAsync();

            return Ok(devices);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // New paginated endpoints
    [HttpGet("devices/local/paged")]
    [ProducesResponseType(typeof(HttpResponseData<PagedResult<DeviceDto>>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> GetLocalDevicesPaged([FromQuery] DevicePagedRequest request)
    {
        try
        {
            var result = await _deviceRepo.GetDevicesPagedAsync(request);

            return Ok(new HttpResponseData<PagedResult<DeviceDto>>
            {
                Result = result,
                Success = true,
                Message = "Devices retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message,
                Error = ex.InnerException?.Message
            });
        }
    }

    [HttpGet("template/local/paged")]
    [ProducesResponseType(typeof(HttpResponseData<PagedResult<TemplateDto>>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> GetLocalTemplatePaged([FromQuery] TemplatePagedRequest request)
    {
        try
        {
            var result = await _deviceRepo.GetTemplatesPagedAsync(request);

            return Ok(new HttpResponseData<PagedResult<TemplateDto>>
            {
                Result = result,
                Success = true,
                Message = "Templates retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message,
                Error = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Retrieves a Tempalte by ID.
    /// </summary>
    [HttpGet("template/{id}")]
    [ProducesResponseType(typeof(HttpResponseData<TemplateDto>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<TemplateDto>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<TemplateDto>), 500)]
    public async Task<IActionResult> GetTemplateById(string id)
    {
        var response = new HttpResponseData<TemplateDto>();
        try
        {
            var template = await _deviceRepo.GetTempalteByIdAsync(id);
            if (template == null)
            {
                response.Success = false;
                response.Message = $"Template with ID {id} not found.";
                response.ResponsCode = 404;
                return NotFound(response);
            }

            response.Success = true;
            response.Message = "Device retrieved successfully.";
            response.Result = template;
            response.ResponsCode = 200;
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving template ID {id}");
            response.Success = false;
            response.Message = "Failed to retrieve template.";
            response.Error = ex.Message;
            response.ResponsCode = 500;
            return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Retrieves templates by a list of IDs.
    /// </summary>
    [HttpPost("templates/by-ids")]
    [ProducesResponseType(typeof(HttpResponseData<List<TemplateDto>>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<List<TemplateDto>>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<List<TemplateDto>>), 500)]
    public async Task<IActionResult> GetTemplatesByIds([FromBody] List<string> ids)
    {
        var response = new HttpResponseData<List<TemplateDto>>();

        try
        {
            if (ids == null || !ids.Any())
            {
                response.Success = false;
                response.Message = "Template ID list is empty.";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            var templates = await _deviceRepo.GetTemplatesByIdsAsync(ids);

            if (templates == null || !templates.Any())
            {
                response.Success = false;
                response.Message = "No templates found for the provided IDs.";
                response.ResponsCode = 404;
                return NotFound(response);
            }

            response.Success = true;
            response.Message = "Templates retrieved successfully.";
            response.Result = templates;
            response.ResponsCode = 200;

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving templates by IDs");

            response.Success = false;
            response.Message = "Failed to retrieve templates.";
            response.Error = ex.Message;
            response.ResponsCode = 500;

            return Problem(
                title: response.Message,
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }


    // Optional: Update original template endpoint to support simple pagination without breaking changes
    [HttpGet("template/local/v2")]
    public async Task<IActionResult> GetLocalTemplateV2(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? storeId = null,
        [FromQuery] string? searchTerm = null)
    {
        try
        {
            var request = new TemplatePagedRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                StoreId = storeId,
                SearchTerm = searchTerm
            };

            var result = await _deviceRepo.GetTemplatesPagedAsync(request);

            return Ok(new HttpResponseData<PagedResult<TemplateDto>>
            {
                Result = result,
                Success = true,
                Message = "Templates retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message,
                Error = ex.InnerException?.Message
            });
        }
    }

    // ============ TEMPLATE MANAGEMENT ============

    [HttpGet("templates/sync")]
    public async Task<IActionResult> SyncTemplates([FromQuery] long storeId)
    {
        try
        {
            var minewStore = await _context.StoreMaster.FirstOrDefaultAsync(x => x.Id == storeId);
            if(minewStore == null)
            {
                return NotFound($"Store with ID {storeId} was not found.");
            }
            var provider = _eslProviderFactory.GetProvider("Minew");
            var response = await provider.GetTemplatesFromCloudAsync(minewStore.MinewStoreId);

            if (response == null || response.Code != 200)
                return BadRequest(new { message = response?.Msg ?? "Cloud error" });

            if (response.Data?.Rows == null || response.Data.Rows.Count == 0)
                return Ok(new { message = "No templates found", templates = new List<object>() });

            foreach (var template in response.Data.Rows)
            {
                if (string.IsNullOrEmpty(template.DemoId))
                    continue;

                var existing = await _context.MinewTemplates
                    .FirstOrDefaultAsync(t => t.Id == template.DemoId);

                if (existing == null)
                {
                    _context.MinewTemplates.Add(new MinewTemplates
                    {
                        Id = template.DemoId,
                        Name = template.DemoName ?? "Unnamed Template",
                        Description = template.DemoName ?? "No description",
                        Color = template.Color,
                        Orientation = template.Orientation,
                        ScreenInch = template.ScreenSize?.Inch ?? 0,
                        ScreenWidth = template.ScreenSize?.Width ?? 0,
                        ScreenHeight = template.ScreenSize?.Height ?? 0,
                        StoreId = storeId,
                        IsActive = true,
                        SyncDate = DateTime.UtcNow,
                        CreatedDate = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            var templates = await _context.MinewTemplates
                .Where(t => t.StoreId == storeId && t.IsActive)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Description,
                    t.ScreenInch,
                    t.ScreenWidth,
                    t.ScreenHeight,
                    t.Color,
                    t.Orientation,
                    t.StoreId
                })
                .ToListAsync();

            return Ok(templates);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("templates/preview")]
    public async Task<IActionResult> GetTemplatePreview([FromBody] TemplatePreviewRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.TemplateId))
                return BadRequest(new { message = "TemplateId is required" });

            var provider = _eslProviderFactory.GetProvider("Minew");
            string previewData;

            if (request.isBound && !string.IsNullOrEmpty(request.Mac) && !string.IsNullOrEmpty(request.StoreId))
            {
                previewData = await provider.GetBoundTemplatePreviewAsync(request.TemplateId, request.Mac, request.StoreId);
            }
            else
            {
                previewData = await provider.GetUnboundTemplatePreviewAsync(request.TemplateId);
            }

            // Update template with preview image
            var template = await _context.MinewTemplates
                .FirstOrDefaultAsync(t => t.Id == request.TemplateId);

            if (template != null)
            {
                template.PreviewImage = previewData;
                await _context.SaveChangesAsync();
            }

            return Ok(new { data = previewData });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpDelete("template/{id}/user/{userId}")]
    [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> DeleteTemplate(string id, int userId)
    {
        try
        {
            var (success, message) = await _deviceRepo.DeleteTemplateAsync(id, userId);

            if (!success)
            {
                // Cannot delete due to active combos
                return Ok(new HttpResponseData<bool>
                {
                    Success = false,
                    Message = message,
                    Result = false
                });
            }

            return Ok(new HttpResponseData<bool>
            {
                Success = true,
                Message = message,
                Result = true
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "template not found for deletion");
            return NotFound(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while deleting the template",
                Error = ex.Message
            });
        }
    }

    #endregion

    #region Device - Template Combination

    [HttpPost("combos")]
    public async Task<IActionResult> CreateCombo([FromBody] CreateComboRequest request)
    {
        try
        {
            if (request.DeviceId <= 0)
                return BadRequest(new { message = "DeviceId is required" });
            if (string.IsNullOrEmpty(request.TemplateId))
                return BadRequest(new { message = "TemplateId is required" });

            var (combo, alreadyExisted) = await _deviceRepo.CreateOrReuseTemplateComboAsync(
                request.DeviceId, request.TemplateId, request.IsDefault);

            var device = await _context.DeviceMaster.FirstOrDefaultAsync(d => d.Id == combo.DeviceId);
            var template = await _context.MinewTemplates.FirstOrDefaultAsync(t => t.Id == combo.TemplateId);

            return Ok(new
            {
                Id = combo.Id,
                DeviceId = combo.DeviceId,
                TemplateId = combo.TemplateId,
                DeviceName = device?.Name,
                TemplateName = template?.Name,
                ScreenWidth = template?.ScreenWidth,
                ScreenHeight = template?.ScreenHeight,
                IsDefault = combo.IsDefault,
                Priority = combo.Priority,
                CreatedDate = combo.CreatedDate,
                AlreadyExists = alreadyExisted
            });
        }
        catch (DeviceRepository.NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
    /// <summary>
    /// Get Device and Template Combinations
    /// </summary>
    /// <returns></returns>
    [HttpGet("combos")]
    public async Task<IActionResult> GetDeviceTemplateCombos()
    {
        try
        {
            var combos = await _context.DeviceTemplateCombos
                .Join(_context.DeviceMaster,
                    combo => combo.DeviceId,
                    device => device.Id,
                    (combo, device) => new { combo, device })
                .Join(_context.MinewTemplates,
                    x => x.combo.TemplateId,
                    template => template.Id,
                    (x, template) => new
                    {
                        Id = x.combo.Id,
                        DeviceId = x.combo.DeviceId,
                        TemplateId = x.combo.TemplateId,
                        DeviceName = x.device.Name,
                        TemplateName = template.Name,
                        ScreenWidth = template.ScreenWidth,
                        ScreenHeight = template.ScreenHeight,
                        ScreenInch = template.ScreenInch,
                        Battery = x.device.Battery,
                        IsDefault = x.combo.IsDefault,
                        Priority = x.combo.Priority,
                        CreatedDate = x.combo.CreatedDate
                    })
                .OrderByDescending(c => c.IsDefault)
                .ThenBy(c => c.Priority)
                .ToListAsync();

            return Ok(combos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get paginated Device and Template Combinations
    /// </summary>
    [HttpGet("combos/paged")]
    [ProducesResponseType(typeof(HttpResponseData<PagedResult<DeviceTemplateComboDto>>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> GetDeviceTemplateCombosPaged([FromQuery] DeviceTemplateComboPagedRequest request)
    {
        try
        {
            var result = await _deviceRepo.GetCombosPagedAsync(request);

            return Ok(new HttpResponseData<PagedResult<DeviceTemplateComboDto>>
            {
                Result = result,
                Success = true,
                Message = "Device template combos retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            var response = new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message,
                Error = ex.InnerException?.Message
            };
            return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get DeviceTemplateCombo by Id
    /// </summary>
    [HttpGet("combos/{id}")]
    [ProducesResponseType(typeof(HttpResponseData<DeviceTemplateComboDto>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> GetDeviceTemplateById(long id)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Error = ModelState.Values.ToString()
                });
            }

            var result = await _deviceRepo.GetComboByIdAsync(id);

            return Ok(new HttpResponseData<DeviceTemplateComboDto>
            {
                Result = result,
                Success = true,
                Message = "DeviceTemplateCombo updated successfully"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DeviceTemplateCombo with ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while updating DeviceTemplateCombo",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Update an existing DeviceTemplateCombo
    /// </summary>
    [HttpPut("combos/{id}")]
    [ProducesResponseType(typeof(HttpResponseData<DeviceTemplateCombos>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 409)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> UpdateDeviceTemplateCombo(int id, [FromBody] UpdateDeviceTemplateComboDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Error = ModelState.Values.ToString()
                });
            }

            var result = await _deviceRepo.UpdateDeviceTemplateCombosAsync(id, dto);

            return Ok(new HttpResponseData<DeviceTemplateCombos>
            {
                Result = result,
                Success = true,
                Message = "DeviceTemplateCombo updated successfully"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DeviceTemplateCombo with ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while updating DeviceTemplateCombo",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Update an existing DeviceTemplateCombo
    /// </summary>
    [HttpPost("combos/{id}")]
    [ProducesResponseType(typeof(HttpResponseData<DeviceTemplateCombos>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 409)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateDeviceTemplateComboDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Error = ModelState.Values.ToString()
                });
            }

            var result = await _deviceRepo.UpdateDeviceTemplateCombosAsync(id, dto);

            return Ok(new HttpResponseData<DeviceTemplateCombos>
            {
                Result = result,
                Success = true,
                Message = "DeviceTemplateCombo updated successfully"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DeviceTemplateCombo with ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while updating DeviceTemplateCombo",
                Error = ex.Message
            });
        }
    }

    [HttpDelete("combos/{id}/user/{userId}")]
    [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> DeleteCombo(long id, int userId)
    {
        try
        {
            var (success, message) = await _deviceRepo.DeleteComboAsync(id, userId);

            if (!success)
            {
                // Cannot delete due to active combos
                return Ok(new HttpResponseData<bool>
                {
                    Success = false,
                    Message = message,
                    Result = false
                });
            }

            return Ok(new HttpResponseData<bool>
            {
                Success = true,
                Message = message,
                Result = true
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Combo not found for deletion");
            return NotFound(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting combo ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while deleting the combo",
                Error = ex.Message
            });
        }
    }

    #endregion

    #region Device - Message Combination Handers
    /// <summary>
    /// Get DeviceMessageCombo by ID
    /// </summary>
    [HttpGet("messagecombo/{id}")]
    [ProducesResponseType(typeof(HttpResponseData<DeviceMessageCombos>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> GetById(long id)
    {
        try
        {
            var result = await _deviceRepo.GetByIdAsync(id);

            if (result == null)
            {
                return NotFound(new HttpResponseData<object>
                {
                    Success = false,
                    Message = $"DeviceMessageCombo with ID {id} not found"
                });
            }

            return Ok(new HttpResponseData<DeviceMessageCombos>
            {
                Result = result,
                Success = true,
                Message = "DeviceMessageCombo retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting DeviceMessageCombo by ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while processing your request",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get all active DeviceMessageCombos
    /// </summary>
    [HttpGet("messagecombo")]
    [ProducesResponseType(typeof(HttpResponseData<DeviceMessageCombos>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _deviceRepo.GetAllAsync();

            return Ok(new HttpResponseData<DeviceMessageCombos>
            {
                Results = result,
                Success = true,
                Message = $"Retrieved {result.Count} DeviceMessageCombos"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all DeviceMessageCombos");
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while processing your request",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get paginated DeviceMessageCombos
    /// </summary>
    [HttpGet("messagecombo/paged")]
    [ProducesResponseType(typeof(HttpResponseData<PagedResult<DeviceMessageComboDto>>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> GetPaged([FromQuery] DeviceMessageComboPagedRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Invalid request parameters",
                    Error = ModelState.Values.ToString()
                });
            }

            var result = await _deviceRepo.GetPagedAsync(request);

            return Ok(new HttpResponseData<PagedResult<DeviceMessageComboDto>>
            {
                Result = result,
                Success = true,
                Message = $"Retrieved {result.Items.Count} of {result.TotalCount} DeviceMessageCombos"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paginated DeviceMessageCombos");
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while processing your request",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Create a new DeviceMessageCombo
    /// </summary>
    [HttpPost("messagecombo")]
    [ProducesResponseType(typeof(HttpResponseData<DeviceMessageCombos>), 201)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 409)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> Create([FromBody] CreateDeviceMessageComboDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Error = ModelState.Values.ToString()
                });
            }

            var result = await _deviceRepo.CreateAsync(dto);

            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new HttpResponseData<DeviceMessageCombos>
                {
                    Result = result,
                    Success = true,
                    Message = "DeviceMessageCombo created successfully"
                });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating DeviceMessageCombo");
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while creating DeviceMessageCombo",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Update an existing DeviceMessageCombo
    /// </summary>
    [HttpPut("messagecombo/{id}")]
    [ProducesResponseType(typeof(HttpResponseData<DeviceMessageCombos>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 409)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateDeviceMessageComboDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Error = ModelState.Values.ToString()
                });
            }

            var result = await _deviceRepo.UpdateAsync(id,dto);

            return Ok(new HttpResponseData<DeviceMessageCombos>
            {
                Result = result,
                Success = true,
                Message = "DeviceMessageCombo updated successfully"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DeviceMessageCombo with ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while updating DeviceMessageCombo",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Delete a DeviceMessageCombo
    /// </summary>
    [HttpDelete("messagecombo/{id}")]
    [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            var result = await _deviceRepo.DeleteAsync(id);

            if (!result)
            {
                return NotFound(new HttpResponseData<object>
                {
                    Success = false,
                    Message = $"DeviceMessageCombo with ID {id} not found"
                });
            }

            return Ok(new HttpResponseData<object>
            {
                Success = true,
                Message = "DeviceMessageCombo deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting DeviceMessageCombo with ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while deleting DeviceMessageCombo",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get DeviceMessageCombos by Device ID
    /// </summary>
    [HttpGet("messagecombo/device/{deviceId}")]
    [ProducesResponseType(typeof(HttpResponseData<DeviceMessageCombos>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> GetByDeviceId(long deviceId)
    {
        try
        {
            var result = await _deviceRepo.GetByDeviceIdAsync(deviceId);

            return Ok(new HttpResponseData<DeviceMessageCombos>
            {
                Results = result,
                Success = true,
                Message = $"Retrieved {result.Count} DeviceMessageCombos for Device ID: {deviceId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting DeviceMessageCombos by DeviceId: {DeviceId}", deviceId);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while processing your request",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get DeviceMessageCombos by Message ID
    /// </summary>
    [HttpGet("messagecombo/message/{messageId}")]
    [ProducesResponseType(typeof(HttpResponseData<DeviceMessageCombos>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> GetByMessageId(long messageId)
    {
        try
        {
            var result = await _deviceRepo.GetByMessageIdAsync(messageId);

            return Ok(new HttpResponseData<DeviceMessageCombos>
            {
                Results = result,
                Success = true,
                Message = $"Retrieved {result.Count} DeviceMessageCombos for Message ID: {messageId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting DeviceMessageCombos by MessageId: {MessageId}", messageId);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while processing your request",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Check if DeviceMessageCombo exists
    /// </summary>
    [HttpGet("messagecombo/exists")]
    [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> Exists([FromQuery] long deviceId, [FromQuery] long messageId)
    {
        try
        {
            if (deviceId <= 0 || messageId <= 0)
            {
                return BadRequest(new HttpResponseData<object>
                {
                    Success = false,
                    Message = "DeviceId and MessageId must be greater than 0"
                });
            }

            var result = await _deviceRepo.ExistsAsync(deviceId, messageId);

            return Ok(new HttpResponseData<bool>
            {
                Result = result,
                Success = true,
                Message = result ? "DeviceMessageCombo exists" : "DeviceMessageCombo does not exist"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if DeviceMessageCombo exists");
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while processing your request",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get total count of DeviceMessageCombos
    /// </summary>
    [HttpGet("messagecombo/count")]
    [ProducesResponseType(typeof(HttpResponseData<int>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> GetTotalCount()
    {
        try
        {
            var count = await _deviceRepo.GetTotalCountAsync();

            return Ok(new HttpResponseData<int>
            {
                Result = count,
                Success = true,
                Message = $"Total DeviceMessageCombos: {count}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total count of DeviceMessageCombos");
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while processing your request",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Deactivate all DeviceMessageCombos by Device ID
    /// </summary>
    [HttpPut("messagecombo/device/{deviceId}/deactivate")]
    [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> DeactivateByDeviceId(long deviceId)
    {
        try
        {
            var result = await _deviceRepo.DeactivateByDeviceIdAsync(deviceId);

            return Ok(new HttpResponseData<object>
            {
                Success = true,
                Message = result
                    ? $"Deactivated all DeviceMessageCombos for Device ID: {deviceId}"
                    : $"No active DeviceMessageCombos found for Device ID: {deviceId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating DeviceMessageCombos by DeviceId: {DeviceId}", deviceId);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while processing your request",
                Error = ex.Message
            });
        }
    }

    ///// <summary>
    ///// Create multiple DeviceMessageCombos in bulk
    ///// </summary>
    //[HttpPost("bulk")]
    //[ProducesResponseType(typeof(HttpResponseData<List<DeviceMessageCombos>>), 201)]
    //[ProducesResponseType(typeof(HttpResponseData<object>), 400)]
    //[ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    //public async Task<IActionResult> CreateBulk([FromBody] List<CreateDeviceMessageComboDto> dtos)
    //{
    //    try
    //    {
    //        if (!ModelState.IsValid || dtos == null || !dtos.Any())
    //        {
    //            return BadRequest(new HttpResponseData<object>
    //            {
    //                Success = false,
    //                Message = "Invalid request data or empty list"
    //            });
    //        }

    //        var result = await _deviceRepo.CreateBulkAsync(dtos);

    //        return CreatedAtAction(nameof(GetAll), null,
    //            new HttpResponseData<List<DeviceMessageCombos>>
    //            {
    //                Results = result,
    //                Success = true,
    //                Message = $"Created {result.Count} DeviceMessageCombos successfully"
    //            });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error creating bulk DeviceMessageCombos");
    //        return StatusCode(500, new HttpResponseData<object>
    //        {
    //            Success = false,
    //            Message = "An error occurred while creating bulk DeviceMessageCombos",
    //            Error = ex.Message
    //        });
    //    }
    //}

    /// <summary>
    /// Create multiple DeviceMessageCombos in bulk
    /// </summary>
    [HttpPost("messagecombo/bulk")]
    [ProducesResponseType(typeof(HttpResponseData<DeviceMessageCombos>), 201)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> CreateBulk([FromBody] List<CreateDeviceMessageComboDto> dtos)
    {
        try
        {
            if (!ModelState.IsValid || dtos == null || !dtos.Any())
            {
                return BadRequest(new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Invalid request data or empty list"
                });
            }

            var result = await _deviceRepo.CreateBulkAsync(dtos);
            var resutIds = result.Select(r => r.Id).ToList();
            return CreatedAtAction(nameof(GetAll), null,
                new HttpResponseData<DeviceMessageCombos>
                {
                    Results = result,
                    Success = true,
                    Message = $"Created {result.Count} DeviceMessageCombos successfully"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk DeviceMessageCombos");
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while creating bulk DeviceMessageCombos",
                Error = ex.Message
            });
        }
    }


    /// <summary>
    /// Deactivate multiple DeviceMessageCombos
    /// </summary>
    [HttpPut("messagecombo/deactivate-multiple")]
    [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> DeactivateMultiple([FromBody] List<long> ids)
    {
        try
        {
            if (ids == null || !ids.Any())
            {
                return BadRequest(new HttpResponseData<object>
                {
                    Success = false,
                    Message = "No IDs provided"
                });
            }

            var result = await _deviceRepo.DeactivateMultipleAsync(ids);

            return Ok(new HttpResponseData<object>
            {
                Success = true,
                Message = result
                    ? $"Deactivated {ids.Count} DeviceMessageCombos"
                    : "No active DeviceMessageCombos found with the provided IDs"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating multiple DeviceMessageCombos");
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while deactivating DeviceMessageCombos",
                Error = ex.Message
            });
        }
    }

    [HttpDelete("messagecombo/{id}/user/{userId}")]
    [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> DeleteMessageCombo(long id, int userId)
    {
        try
        {
            var (success, message) = await _deviceRepo.DeleteMessageComboAsync(id, userId);

            if (!success)
            {
                // Cannot delete due to active combos
                return Ok(new HttpResponseData<bool>
                {
                    Success = false,
                    Message = message,
                    Result = false
                });
            }

            return Ok(new HttpResponseData<bool>
            {
                Success = true,
                Message = message,
                Result = true
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Message combo not found for deletion");
            return NotFound(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message combo ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while deleting the message combo",
                Error = ex.Message
            });
        }
    }
    #endregion


    #region Combination Assignments(to shelf or product) Handlers 
    // ============ ASSIGNMENTS (Using DeviceTemplateAssignment Table) ============
    [HttpPost("assignments")]
    [ProducesResponseType(typeof(HttpResponseData<AssignmentDto>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentRequest request)
    {
        try
        {
            var result = await _deviceRepo.CreateAssignmentAsync(request);
            return Ok(new HttpResponseData<AssignmentDto>
            {
                Success = true,
                Message = "Assignment created successfully",
                ResponsCode = 200,
                Result = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating assignment");
            return new ObjectResult(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message,
                ResponsCode = 400
            })
            {
                StatusCode = 400
            };
        }
        catch (DeviceRepository.NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found creating assignment");
            return new ObjectResult(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message,
                ResponsCode = 404
            })
            {
                StatusCode = 404
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation creating assignment");
            return new ObjectResult(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message,
                ResponsCode = 400
            })
            {
                StatusCode = 400
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assignment");
            return new ObjectResult(new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while creating the assignment",
                ResponsCode = 500,
                Error = ex.Message
            })
            {
                StatusCode = 500
            };
        }
    }

    [HttpGet("assignments/paged")]
    [ProducesResponseType(typeof(HttpResponseData<PagedResult<AssignmentViewModel>>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> GetAssignmentsPaged([FromQuery] AssignmentPagedRequest request)
    {
        try
        {
            var result = await _deviceRepo.GetAssignmentsPagedAsync(request);

            return Ok(new HttpResponseData<PagedResult<AssignmentViewModel>>
            {
                Success = true,
                Message = "Assignments retrieved successfully",
                ResponsCode = 200,
                Result = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated assignments");
            return new ObjectResult(new HttpResponseData<object>
            {
                Success = false,
                Message = "Error retrieving assignments",
                ResponsCode = 500,
                Error = ex.Message
            })
            {
                StatusCode = 500
            };
        }
    }

    [HttpGet("assignments/{locationType}/{locationId}")]
    [ProducesResponseType(typeof(HttpResponseData<List<AssignmentDto>>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> GetAssignments(string locationType, long locationId)
    {
        try
        {
            var assignments = await _deviceRepo.GetAssignmentsAsync(locationType, locationId)
                               ?? new List<AssignmentDto>();

            return Ok(new HttpResponseData<List<AssignmentDto>>
            {
                Success = true,
                Message = assignments.Count == 0
                    ? "No assignments found"
                    : "Assignments retrieved successfully",
                ResponsCode = 200,
                Result = assignments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assignments");

            return new ObjectResult(new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while retrieving assignments",
                ResponsCode = 500,
                Error = ex.Message
            })
            {
                StatusCode = 500
            }; ;
        }
    }


    [HttpPut("assignments/{id}/order")]
    [ProducesResponseType(typeof(HttpResponseData<AssignmentDto>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> UpdateAssignmentOrder(
        long id,
        [FromBody] UpdateAssignmentOrderRequest request)
    {
        try
        {
            var result = await _deviceRepo.UpdateAssignmentOrderAsync(id, request);
            return Ok(new HttpResponseData<AssignmentDto>
            {
                Success = true,
                Message = "Assignment order updated successfully",
                ResponsCode = 200,
                Result = result
            });
        }
        catch (DeviceRepository.NotFoundException ex)
        {
            _logger.LogWarning(ex, "Assignment not found for order update");
            return new ObjectResult(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message,
                ResponsCode = 404
            })
            {
                StatusCode = 404
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assignment order");
            return new ObjectResult(new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while updating assignment order",
                ResponsCode = 500,
                Error = ex.Message
            })
            {
                StatusCode = 500
            };
        }
    }

    [HttpDelete("assignments/{id}")]
    [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 401)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> RemoveAssignment(long id, int userId)
    {
        try
        {
            //// Get current user ID from claims
            //var userIdClaim = User?.FindFirst("UserId");
            //if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            //{
            //    return new ObjectResult(new HttpResponseData<object>
            //    {
            //        Success = false,
            //        Message = "User not authenticated",
            //        ResponsCode = 401
            //    })
            //    {
            //        StatusCode = 401
            //    };
            //}

            var success = await _deviceRepo.RemoveAssignmentAsync(id, userId);

            if (!success)
            {
                return new ObjectResult(new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Assignment not found",
                    ResponsCode = 404
                })
                {
                    StatusCode = 404
                };
            }

            return Ok(new HttpResponseData<object>
            {
                Success = true,
                Message = "Assignment removed successfully",
                ResponsCode = 200
            });
        }
        catch (DeviceRepository.NotFoundException ex)
        {
            _logger.LogWarning(ex, "Assignment not found for removal");
            return new ObjectResult(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message,
                ResponsCode = 404
            })
            {
                StatusCode = 404
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing assignment");
            return new ObjectResult(new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while removing the assignment",
                ResponsCode = 500,
                Error = ex.Message
            })
            {
                StatusCode = 500
            };
        }
    }

    #endregion

    #region Paged Endpoints

    //[HttpGet("assignments/paged")]
    //[ProducesResponseType(typeof(HttpResponseData<PagedResult<AssignmentDto>>), 200)]
    //[ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    //public async Task<IActionResult> GetAssignmentsPaged([FromQuery] AssignmentPagedRequest request)
    //{
    //    try
    //    {
    //        var result = await _deviceRepo.GetAssignmentsPagedAsync(request);

    //        return Ok(new HttpResponseData<PagedResult<AssignmentDto>>
    //        {
    //            Success = true,
    //            Message = "Assignments retrieved successfully",
    //            ResponsCode = 200,
    //            Result = result
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error retrieving paginated assignments");
    //        return new ObjectResult(new HttpResponseData<object>
    //        {
    //            Success = false,
    //            Message = "Error retrieving assignments",
    //            ResponsCode = 500,
    //            Error = ex.Message
    //        })
    //        {
    //            StatusCode = 500
    //        };
    //    }
    //}

    [HttpGet("assignments/{locationType}/{locationId}/paged")]
    [ProducesResponseType(typeof(HttpResponseData<PagedResult<AssignmentViewModel>>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> GetAssignmentsByLocationPaged(
        string locationType,
        long locationId,
        [FromQuery] BasePagedRequest request)
    {
        try
        {
            var assignmentRequest = new AssignmentPagedRequest
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                SortDescending = request.SortDescending,
                SearchTerm = request.SearchTerm,
                LocationType = locationType,
                LocationId = locationId
            };

            var result = await _deviceRepo.GetAssignmentsPagedAsync(assignmentRequest);

            return Ok(new HttpResponseData<PagedResult<AssignmentViewModel>>
            {
                Success = true,
                Message = "Assignments retrieved successfully",
                ResponsCode = 200,
                Result = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated assignments by location");
            return new ObjectResult(new HttpResponseData<object>
            {
                Success = false,
                Message = "Error retrieving assignments",
                ResponsCode = 500,
                Error = ex.Message
            })
            {
                StatusCode = 500
            };
        }
    }

    [HttpGet("devices/paged")]
    [ProducesResponseType(typeof(HttpResponseData<PagedResult<DeviceDto>>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> GetDevicesPaged([FromQuery] DevicePagedRequest request)
    {
        try
        {
            var result = await _deviceRepo.GetDevicesPagedAsync(request);

            return Ok(new HttpResponseData<PagedResult<DeviceDto>>
            {
                Success = true,
                Message = "Devices retrieved successfully",
                ResponsCode = 200,
                Result = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated devices");
            return new ObjectResult(new HttpResponseData<object>
            {
                Success = false,
                Message = "Error retrieving devices",
                ResponsCode = 500,
                Error = ex.Message
            })
            {
                StatusCode = 500
            };
        }
    }

    [HttpGet("templates/paged")]
    [ProducesResponseType(typeof(HttpResponseData<PagedResult<TemplateDto>>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    public async Task<IActionResult> GetTemplatesPaged([FromQuery] TemplatePagedRequest request)
    {
        try
        {
            var result = await _deviceRepo.GetTemplatesPagedAsync(request);

            return Ok(new HttpResponseData<PagedResult<TemplateDto>>
            {
                Success = true,
                Message = "Templates retrieved successfully",
                ResponsCode = 200,
                Result = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated templates");
            return new ObjectResult(new HttpResponseData<object>
            {
                Success = false,
                Message = "Error retrieving templates",
                ResponsCode = 500,
                Error = ex.Message
            })
            {
                StatusCode = 500
            };
        }
    }

    //[HttpGet("combos/paged")]
    //[ProducesResponseType(typeof(HttpResponseData<PagedResult<DeviceTemplateComboDto>>), 200)]
    //[ProducesResponseType(typeof(HttpResponseData<object>), 500)]
    //public async Task<IActionResult> GetCombosPaged([FromQuery] DeviceTemplateComboPagedRequest request)
    //{
    //    try
    //    {
    //        var result = await _deviceRepo.GetCombosPagedAsync(request);

    //        return Ok(new HttpResponseData<PagedResult<DeviceTemplateComboDto>>
    //        {
    //            Success = true,
    //            Message = "Combos retrieved successfully",
    //            ResponsCode = 200,
    //            Result = result
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error retrieving paginated combos");
    //        return new ObjectResult(new HttpResponseData<object>
    //        {
    //            Success = false,
    //            Message = "Error retrieving combos",
    //            ResponsCode = 500,
    //            Error = ex.Message
    //        })
    //        {
    //            StatusCode = 500
    //        };
    //    }
    //}


    #endregion

    #region Bind Data Handlers

    // ============ BIND DATA ============

    [HttpPost("bind/test-preview")]
    public async Task<IActionResult> TestBindPreview([FromBody] TestBindRequest request)
    {
        try
        {
            if (request.ComboId <= 0)
                return BadRequest(new { message = "Valid ComboId is required" });
            if (request.TestData == null)
                return BadRequest(new { message = "TestData is required" });

            // Get combo details
            var combo = await _context.DeviceTemplateCombos
                .Include(c => c.Device)
                .Include(c => c.Template)
                .FirstOrDefaultAsync(c => c.Id == request.ComboId);

            if (combo == null)
                return NotFound(new { message = "Combo not found" });

            var device = await _context.DeviceMaster.FirstOrDefaultAsync(x => x.Id == combo.DeviceId);
            if (device == null)
                return NotFound(new {message ="Device not found"});

            var store = await _context.StoreMaster.FirstOrDefaultAsync(x => x.Id == device.StoreId);

            if (store == null)
                return NotFound(new { message = "Store not found" });

            var token = await GetToken();

            // Generate preview using template with test data
            var previewData = await _minewService.GetTemplatePreviewWithData(
                token,
                combo.TemplateId,
                request.TestData,
                store.MinewStoreId);

            return Ok(new { previewImage = previewData });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("bind")]
    public async Task<IActionResult> BindData([FromBody] BindDataRequest request)
    {
        try
        {
            if (request.ComboId <= 0)
                return BadRequest(new { message = "Valid ComboId is required" });

            // Get combo details
            var combo = await _context.DeviceTemplateCombos
                .Include(c => c.Device)
                .Include(c => c.Template)
                .FirstOrDefaultAsync(c => c.Id == request.ComboId);

            if (combo == null)
                return NotFound(new { message = "Combo not found" });

            // Determine if we should get product data from request or generate from product ID
            Dictionary<string, string> goodsMap;

            if (request.ProductId.HasValue && request.ProductId > 0)
            {
                // Get product from database
                var product = await _context.ProductMaster
                    .FirstOrDefaultAsync(p => p.Id == request.ProductId.Value);

                if (product == null)
                    return NotFound(new { message = "Product not found" });

                // Generate goodsMap from product
                goodsMap = GenerateGoodsMapFromProduct(product);
            }
            else if (request.GoodsMap != null && request.GoodsMap.Any())
            {
                // Use provided goodsMap
                goodsMap = request.GoodsMap;
            }
            else
            {
                return BadRequest(new { message = "Either ProductId or GoodsMap must be provided" });
            }

            var provider = _eslProviderFactory.GetProvider(combo.Device.DeviceType ?? "Minew");

            // Prepare bind request for the vendor's cloud API
            var bindRequest = new
            {
                storeId = combo.Device.StoreId,
                labelMac = combo.Device.MACAddress,
                goodsMap = goodsMap,
                demoIdMap = new Dictionary<string, string>
                {
                    ["A"] = combo.TemplateId
                },
                color = request.Color ?? 1,
                total = request.Total ?? 5,
                period = request.Period ?? 500,
                interval = request.Interval ?? 900,
                brightness = request.Brightness ?? 100,
                opCode = new Random().Next(1000000000, 2000000000)
            };

            var responseDoc = await provider.BindDataAsync(bindRequest);
            var root = responseDoc.RootElement;

            if (!root.TryGetProperty("code", out var codeElement) || codeElement.GetInt32() != 200)
            {
                var msg = root.TryGetProperty("msg", out var msgElement)
                    ? msgElement.GetString()
                    : "Unknown error";

                return BadRequest(new { message = msg });
            }

            return Ok(new
            {
                message = "Data bound successfully",
                response = root,
                goodsMap
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("bind-unified")]
    public async Task<IActionResult> BindDataUnified([FromBody] UnifiedBindDataRequest request)
    {
        try
        {
            if (request.ComboId <= 0)
                return BadRequest(new { message = "Valid ComboId is required" });

            var isMessageCombo = string.Equals(
                request.ComboType, "MESSAGE", StringComparison.OrdinalIgnoreCase);

            DeviceTemplateCombos combo;

            if (isMessageCombo)
            {
                // A DeviceMessageCombos row carries device + message but no template,
                // and a Minew bind cannot render without one (demoIdMap needs a
                // TemplateId). Resolve the device's own template combo instead, and
                // default the image to this combo's message when none was supplied.
                var messageCombo = await _context.DeviceMessageCombos
                    .Include(c => c.Device)
                    .FirstOrDefaultAsync(c => c.Id == request.ComboId);

                if (messageCombo == null)
                    return NotFound(new { message = "Message combo not found" });

                combo = await _context.DeviceTemplateCombos
                    .Include(c => c.Device)
                    .Include(c => c.Template)
                    .Where(c => c.DeviceId == messageCombo.DeviceId && c.IsActive)
                    .OrderByDescending(c => c.IsDefault)
                    .ThenByDescending(c => c.Priority)
                    .FirstOrDefaultAsync();

                if (combo == null)
                    return BadRequest(new
                    {
                        message =
                            "This device has no template paired with it, so there is " +
                            "nothing for the label to render. Pair a template first."
                    });

                if (request.MessageId <= 0)
                    request.MessageId = (int)messageCombo.MessageId;
            }
            else
            {
                combo = await _context.DeviceTemplateCombos
                    .Include(c => c.Device)
                    .Include(c => c.Template)
                    .FirstOrDefaultAsync(c => c.Id == request.ComboId);

                if (combo == null)
                    return NotFound(new { message = "Combo not found" });
            }

            //Get Minew Store ID
            var store = await _context.StoreMaster
                .FirstOrDefaultAsync(s => s.Id == combo.Device.StoreId);

            Dictionary<string, string> goodsMap;
            string bindingType = "product"; // Default

            // Determine binding type and generate appropriate goodsMap
            if (request.BindingType == "shelf")
            {
                // Shelf binding logic
                bindingType = "shelf";

                // Get message if provided (0 means no image)
                if (request.MessageId > 0)
                {
                    var message = await _context.MessageMaster
                        .FirstOrDefaultAsync(m => m.Id == request.MessageId && m.IsActive);

                    if (message == null)
                        return NotFound(new { message = "Message not found" });

                    // Validate Base64 format
                    if (string.IsNullOrEmpty(message.ContentData) || !IsValidBase64(message.ContentData))
                        return BadRequest(new { message = "Message does not contain valid image data" });

                    // Generate goodsMap for shelf with image
                    goodsMap = GenerateShelfGoodsMap(
                        shelfId: request.ShelfId ?? 0,
                        shelfName: request.ShelfName ?? "Shelf Display",
                        shelfCode: request.ShelfCode ?? "",
                        imageBase64: message.ContentData
                    );
                }
                else
                {
                    // Shelf binding without image
                    goodsMap = GenerateShelfGoodsMap(
                        shelfId: request.ShelfId ?? 0,
                        shelfName: request.ShelfName ?? "Shelf Display",
                        shelfCode: request.ShelfCode ?? "",
                        imageBase64: null
                    );
                }
            }
            else
            {
                // Product binding logic (default)
                bindingType = "product";

                if (request.ProductId.HasValue && request.ProductId > 0)
                {
                    // Get product from database
                    var product = await _context.ProductMaster
                        .FirstOrDefaultAsync(p => p.Id == request.ProductId.Value);

                    if (product == null)
                        return NotFound(new { message = "Product not found" });

                    // Generate base goodsMap from product
                    goodsMap = GenerateGoodsMapFromProduct(product);

                    // Add image if MessageId is provided and > 0
                    if (request.MessageId > 0)
                    {
                        var message = await _context.MessageMaster
                            .FirstOrDefaultAsync(m => m.Id == request.MessageId && m.IsActive);

                        if (message != null && !string.IsNullOrEmpty(message.ContentData) && IsValidBase64(message.ContentData))
                        {
                            goodsMap["image"] = message.ContentData;
                        }
                    }
                }
                else if (request.GoodsMap != null && request.GoodsMap.Any())
                {
                    // Use provided goodsMap
                    goodsMap = request.GoodsMap;

                    // Add image if MessageId is provided
                    if (request.MessageId > 0)
                    {
                        var message = await _context.MessageMaster
                            .FirstOrDefaultAsync(m => m.Id == request.MessageId && m.IsActive);

                        if (message != null && !string.IsNullOrEmpty(message.ContentData) && IsValidBase64(message.ContentData))
                        {
                            goodsMap["image"] = message.ContentData;
                        }
                    }
                }
                else
                {
                    return BadRequest(new { message = "Either ProductId or GoodsMap must be provided for product binding" });
                }
            }

            var token = await GetToken();

            // Prepare bind request for Minew API
            var bindRequest = new
            {
                storeId = store.MinewStoreId,
                labelMac = combo.Device.MACAddress,
                goodsMap = goodsMap,
                demoIdMap = new Dictionary<string, string>
                {
                    ["A"] = combo.TemplateId
                },
                color = request.Color ?? 1,
                total = request.Total ?? 5,
                period = request.Period ?? 500,
                interval = request.Interval ?? 900,
                brightness = request.Brightness ?? 100,
                opCode = new Random().Next(1000000000, 2000000000)
            };

            // Call Minew API to bind data
            var responseDoc = await _minewService.BindData(token, bindRequest);
            var root = responseDoc.RootElement;

            if (!root.TryGetProperty("code", out var codeElement) || codeElement.GetInt32() != 200)
            {
                var msg = root.TryGetProperty("msg", out var msgElement)
                    ? msgElement.GetString()
                    : "Unknown error";

                return BadRequest(new { message = msg });
            }

            return Ok(new
            {
                message = $"Data bound successfully ({bindingType})",
                bindingType = bindingType,
                response = root,
                goodsMap,
                hasImage = !string.IsNullOrEmpty(goodsMap.ContainsKey("image") ? goodsMap["image"] : null)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
  

    [HttpPost("bind-shelf")]
    public async Task<IActionResult> BindShelfData([FromBody] BindShelfDataRequest request)
    {
        try
        {
            if (request.ComboId <= 0)
                return BadRequest(new { message = "Valid ComboId is required" });

            if (request.MessageId <= 0)
                return BadRequest(new { message = "Valid MessageId is required" });

            // Get combo details
            var combo = await _context.DeviceTemplateCombos
                .Include(c => c.Device)
                .Include(c => c.Template)
                .FirstOrDefaultAsync(c => c.Id == request.ComboId);

            if (combo == null)
                return NotFound(new { message = "Combo not found" });

            // Get message details - ContentData already contains Base64
            var message = await _context.MessageMaster
                .FirstOrDefaultAsync(m => m.Id == request.MessageId && m.IsActive);

            if (message == null)
                return NotFound(new { message = "Message not found" });

            // Check if ContentData has Base64 image
            if (string.IsNullOrEmpty(message.ContentData))
                return BadRequest(new { message = "Message does not contain image data" });

            // Validate it's Base64 format
            if (!IsValidBase64(message.ContentData))
                return BadRequest(new { message = "Message content is not valid Base64 format" });

            // For debugging - log Base64 info
            _logger.LogInformation("Message {MessageId} Base64 length: {Length}",
                message.Id, message.ContentData.Length);

            // Generate goodsMap for shelf (id=0 for shelf)
            var goodsMap = new Dictionary<string, string>
            {
                ["id"] = "0", // Shelf identifier
                ["specification"] = "2.9",
                ["unit"] = "001f",
                ["price"] = "0.00",
                ["memberPrice"] = "",
                ["origin"] = "",
                ["discount"] = "0.00",
                ["barcoode"] = "", // typo but required by API
                ["qrcode"] = "",
                ["p_name"] = request.ShelfName ?? "Shelf Display",
                ["p_code"] = request.ShelfCode ?? "",
                ["image"] = message.ContentData // Direct Base64 from ContentData
            };

            var token = await GetToken();

            // Prepare bind request for Minew API
            var bindRequest = new
            {
                storeId = combo.Device.StoreId,
                labelMac = combo.Device.MACAddress,
                goodsMap = goodsMap,
                demoIdMap = new Dictionary<string, string>
                {
                    ["A"] = combo.TemplateId
                },
                color = request.Color ?? 1,
                total = request.Total ?? 5,
                period = request.Period ?? 500,
                interval = request.Interval ?? 900,
                brightness = request.Brightness ?? 100,
                opCode = new Random().Next(1000000000, 2000000000)
            };

            // Call Minew API to bind data
            var responseDoc = await _minewService.BindData(token, bindRequest);
            var root = responseDoc.RootElement;

            if (!root.TryGetProperty("code", out var codeElement) || codeElement.GetInt32() != 200)
            {
                var msg = root.TryGetProperty("msg", out var msgElement)
                    ? msgElement.GetString()
                    : "Unknown error";

                return BadRequest(new { message = msg });
            }

            // Log the binding in your system
            //await LogShelfBinding(combo.Id, request.MessageId, request.ShelfId);

            return Ok(new
            {
                message = "Shelf data bound successfully",
                response = root,
                goodsMap,
                messageTitle = message.Title,
                imageSize = message.ContentData.Length,
                imageFormat = GetBase64ImageFormat(message.ContentData)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error binding shelf data for ComboId: {ComboId}", request.ComboId);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("bind/quick-product")]
    public async Task<IActionResult> QuickBindProduct([FromBody] QuickBindRequest request)
    {
        try
        {
            if (request.ProductId <= 0)
                return BadRequest(new { message = "Valid ProductId is required" });
            if (request.ComboId <= 0)
                return BadRequest(new { message = "Valid ComboId is required" });

            // Get product details
            var product = await _context.ProductMaster
                .FirstOrDefaultAsync(p => p.Id == request.ProductId);

            if (product == null)
                return NotFound(new { message = "Product not found" });

            // Get combo details
            var combo = await _context.DeviceTemplateCombos
                .Include(c => c.Device)
                .Include(c => c.Template)
                .FirstOrDefaultAsync(c => c.Id == request.ComboId);

            if (combo == null)
                return NotFound(new { message = "Combo not found" });

            // Prepare goods map from product
            var goodsMap = GetGoodsMapFromProduct(product);

            var bindRequest = new BindDataRequest
            {
                ComboId = request.ComboId,
                GoodsMap = goodsMap,
                Color = 1,
                Brightness = 100
            };

            return await BindData(bindRequest);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("bind/batch")]
    public async Task<IActionResult> BatchBind([FromBody] BatchBindRequest request)
    {
        try
        {
            if (request.Assignments == null || !request.Assignments.Any())
                return BadRequest(new { message = "Assignments are required" });

            var results = new List<BatchBindResult>();

            foreach (var assignment in request.Assignments)
            {
                try
                {
                    // Get combo details
                    var combo = await _context.DeviceTemplateCombos
                        .Include(c => c.Device)
                        .Include(c => c.Template)
                        .FirstOrDefaultAsync(c => c.Id == assignment.ComboId);

                    if (combo == null)
                    {
                        results.Add(new BatchBindResult
                        {
                            ComboId = assignment.ComboId,
                            Success = false,
                            Message = "Combo not found"
                        });
                        continue;
                    }

                    // Get product or location data
                    Dictionary<string, string> goodsMap;
                    if (assignment.ProductId.HasValue && assignment.ProductId > 0)
                    {
                        var product = await _context.ProductMaster
                            .FirstOrDefaultAsync(p => p.Id == assignment.ProductId);

                        if (product == null)
                        {
                            results.Add(new BatchBindResult
                            {
                                ComboId = assignment.ComboId,
                                Success = false,
                                Message = "Product not found"
                            });
                            continue;
                        }

                        goodsMap = GetGoodsMapFromProduct(product);
                    }
                    else if (!string.IsNullOrEmpty(assignment.LocationType) && assignment.LocationId > 0)
                    {
                        goodsMap = new Dictionary<string, string>
                        {
                            ["id"] = assignment.LocationId.ToString(),
                            ["name"] = await GetLocationName(assignment.LocationType, assignment.LocationId),
                            ["type"] = assignment.LocationType
                        };
                    }
                    else
                    {
                        // Use custom data if provided
                        goodsMap = assignment.CustomData ?? new Dictionary<string, string>();
                    }

                    var token = await GetToken();

                    var bindRequest = new
                    {
                        storeId = combo.Device.StoreId,
                        labelMac = combo.Device.MACAddress,
                        goodsMap = goodsMap,
                        demoIdMap = new Dictionary<string, string>
                        {
                            ["A"] = combo.TemplateId
                        },
                        color = assignment.Color ?? 1,
                        brightness = assignment.Brightness ?? 100
                    };

                    var response = await _minewService.BindData(token, bindRequest);

                    if (response != null)
                    {
                        var jsonDoc = JsonDocument.Parse(response.ToString());
                        var root = jsonDoc.RootElement;

                        if (root.TryGetProperty("code", out JsonElement codeElement) && codeElement.GetInt32() == 200)
                        {
                            results.Add(new BatchBindResult
                            {
                                ComboId = assignment.ComboId,
                                Success = true,
                                Message = "Bound successfully"
                            });
                        }
                        else
                        {
                            var msg = root.TryGetProperty("msg", out JsonElement msgElement) ? msgElement.GetString() : "Unknown error";
                            results.Add(new BatchBindResult
                            {
                                ComboId = assignment.ComboId,
                                Success = false,
                                Message = msg
                            });
                        }
                    }
                    else
                    {
                        results.Add(new BatchBindResult
                        {
                            ComboId = assignment.ComboId,
                            Success = false,
                            Message = "Failed to bind data"
                        });
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new BatchBindResult
                    {
                        ComboId = assignment.ComboId,
                        Success = false,
                        Message = ex.Message
                    });
                }
            }

            return Ok(new
            {
                message = "Batch bind completed",
                results = results,
                successCount = results.Count(r => r.Success),
                failureCount = results.Count(r => !r.Success)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    #endregion

    #region Helper methods

    // Helper method to generate goodsMap from product

    private Dictionary<string, string> GenerateGoodsMapFromProduct(ProductMaster product, string imageBase64 = null)
    {
        return new Dictionary<string, string>
        {
            ["id"] = product.Id.ToString(),
            ["specification"] = "2.9",
            ["unit"] = "001f",
            ["price"] = ((decimal)(product.SellingPrice)).ToString(), // IMPORTANT
            ["memberPrice"] = "",
            ["origin"] = "",
            ["discount"] = product.DiscountedPrice.ToString(), // match Postman
            ["barcoode"] = product.BarCode ?? "", // typo REQUIRED
            ["qrcode"] = "",
            ["p_name"] = product.ProductName,
            ["p_code"] = product.ProductCode ?? "",
            ["image"] = imageBase64 ?? ""
        };
    }
    // Helper method for shelf goodsMap
    private Dictionary<string, string> GenerateShelfGoodsMap(int shelfId, string shelfName, string shelfCode, string imageBase64)
    {
        return new Dictionary<string, string>
        {
            ["id"] = $"S-{shelfId}",
            ["specification"] = "2.9",
            ["unit"] = "001f",
            ["price"] = "0.00",
            ["memberPrice"] = "",
            ["origin"] = "",
            ["discount"] = "0.00",
            ["barcoode"] = "", // typo but required by API
            ["qrcode"] = "",
            ["p_name"] = shelfName,
            ["p_code"] = shelfCode ?? "",
            ["image"] = imageBase64 ?? "" // Optional image for shelf
        };
    }

    private bool IsValidBase64(string base64String)
    {
        try
        {
            if (string.IsNullOrEmpty(base64String))
                return false;

            // Remove data URI prefix if present
            if (base64String.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                int base64Index = base64String.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
                if (base64Index > 0)
                {
                    base64String = base64String[(base64Index + 7)..];
                }
            }

            // Try to decode
            byte[] data = Convert.FromBase64String(base64String);

            // Check minimum size
            return data.Length > 100; // At least 100 bytes for a valid image
        }
        catch
        {
            return false;
        }
    }

    private string GetBase64ImageFormat(string base64String)
    {
        try
        {
            // Remove data URI prefix
            if (base64String.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                int base64Index = base64String.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
                if (base64Index > 0)
                {
                    // Extract MIME type from data URI
                    string mimePart = base64String[5..base64Index];
                    int semicolonIndex = mimePart.IndexOf(';');
                    if (semicolonIndex > 0)
                    {
                        return mimePart[..semicolonIndex];
                    }
                    return mimePart.TrimEnd(';');
                }
            }

            // Decode first few bytes to determine format
            string cleanBase64 = base64String;
            if (base64String.Contains("base64,"))
            {
                int base64Index = base64String.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
                cleanBase64 = base64String[(base64Index + 7)..];
            }

            byte[] data = Convert.FromBase64String(cleanBase64);

            if (data.Length < 4)
                return "unknown";

            // Check for common image signatures
            if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
                return "image/jpeg";
            else if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                return "image/png";
            else if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46)
                return "image/gif";
            else if (data[0] == 0x42 && data[1] == 0x4D)
                return "image/bmp";
            else if (data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46)
                return "image/webp";
            else
                return "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    private string GetImageFormat(string base64String)
    {
        try
        {
            // Decode first few bytes to determine format
            byte[] data = Convert.FromBase64String(base64String);

            if (data.Length < 4)
                return "unknown";

            // Check for common image signatures
            if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
                return "jpg/jpeg";
            else if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                return "png";
            else if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46)
                return "gif";
            else if (data[0] == 0x42 && data[1] == 0x4D)
                return "bmp";
            else if (data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46)
                return "webp";
            else
                return "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    private string GetMimeType(string fileName)
    {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }

    private decimal CalculateDiscount(ProductMaster product)
    {
        if (product.SellingPrice <= 0 || product.DiscountPrice <= 0)
            return 0;

        var discountPercentage = ((product.SellingPrice - product.DiscountPrice) / product.SellingPrice) * 100;
        return Math.Round(discountPercentage, 0);
    }

    private string GenerateQRCode(ProductMaster product)
    {
        // Generate QR code string - you can customize this
        // For example: product URL, detailed product info, etc.
        return $"PRODUCT:{product.Id}:{product.ProductCode}";
    }

    private async Task<bool> ValidateLocationExists(string locationType, long locationId)
    {
        switch (locationType.ToLower())
        {
            case "aisle":
                return await _context.AisleMaster.AnyAsync(a => a.Id == locationId && a.IsActive);
            case "shelf":
                return await _context.ShelfMaster.AnyAsync(s => s.Id == locationId && s.IsActive);
            case "product":
                return await _context.ProductMaster.AnyAsync(p => p.Id == locationId && p.IsActive);
            default:
                return false;
        }
    }

    private async Task<string> GetLocationName(string locationType, long locationId)
    {
        switch (locationType.ToLower())
        {
            case "aisle":
                var aisle = await _context.AisleMaster
                    .FirstOrDefaultAsync(a => a.Id == locationId);
                return aisle?.Name ?? "Unknown Aisle";
            case "shelf":
                var shelf = await _context.ShelfMaster
                    .FirstOrDefaultAsync(s => s.Id == locationId);
                return shelf?.Name ?? "Unknown Shelf";
            case "product":
                var product = await _context.ProductMaster
                    .FirstOrDefaultAsync(p => p.Id == locationId);
                return product?.ProductName ?? "Unknown Product";
            default:
                return "Unknown Location";
        }
    }

    private async Task<string> GetToken()
    {
        try
        {
            // Cached for the token's lifetime and sourced from the MinewLogin
            // config section - this used to log in on every single call with
            // credentials hardcoded here.
            return await _minewService.GetValidTokenAsync();
        }
        catch (Exception)
        {
            throw new Exception("Failed to get authentication token. Please check login credentials.");
        }
    }

    #endregion
}







