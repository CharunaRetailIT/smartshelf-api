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

    #region Device Handlers

    /// <summary>
    /// Pulls the device list for a store down from the Minew cloud and updates the local
    /// records. eqstatus filters by cloud device status (default 1,2,8,9).
    /// </summary>
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

            // Loaded once so hand-added devices can be matched by MAC below.
            // These stay tracked, so edits to them are saved with everything else.
            var storeDevices = await _context.DeviceMaster
                .Where(d => d.StoreId == storeMaster.Id)
                .ToListAsync();

            foreach (var device in response.Items)
            {
                if (string.IsNullOrEmpty(device.Id))
                    continue;

                // Match on the cloud id first, then fall back to the MAC.
                // A device added by hand carries MinewDeviceId = its MAC (see
                // CreateDeviceAsync), so matching on the cloud id alone never
                // recognised it and every sync inserted a duplicate row - the
                // original keeping ScreenId null forever.
                var normalisedMac = NormaliseMac(device.Mac);

                var existing = await _context.DeviceMaster
                    .FirstOrDefaultAsync(d => d.MinewDeviceId == device.Id);

                if (existing == null && normalisedMac.Length > 0)
                {
                    // Matched in memory rather than in SQL: chained Replace calls
                    // are not reliably translatable, and a store holds few enough
                    // devices that one load beats a query per cloud device.
                    existing = storeDevices.FirstOrDefault(d =>
                        NormaliseMac(d.MACAddress) == normalisedMac);

                    if (existing != null)
                    {
                        // Repair the link and store the MAC in the canonical form
                        // so the next sync matches on the cloud id directly.
                        existing.MinewDeviceId = device.Id;
                        existing.MACAddress = normalisedMac;
                    }
                }

                DateTime.TryParse(device.Lastupdate, out var lastSeen);

                // Handle DeviceScreen lookup or creation
                long? screenId = null;
                if (device.ScreenInfo != null)
                {
                    // Width + height identify the panel. Inch is deliberately not
                    // part of the match: Minew sometimes omits it, and requiring
                    // it created a second DeviceScreen row for a size already in
                    // the table. Reuse the existing row when the dimensions are
                    // already known; only insert when they are not.
                    var deviceScreen = await _context.DeviceScreens
                        .FirstOrDefaultAsync(ds => ds.Width == device.ScreenInfo.Width &&
                                                   ds.Height == device.ScreenInfo.Height &&
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
                    // ScreenId is optional, so this is a LEFT JOIN and these
                    // columns come back NULL for any device saved without a
                    // screen. Inch/Width/Height are non-nullable on DeviceScreen,
                    // and materialising NULL into them threw "Nullable object
                    // must have a value" - failing the whole sync.
                    ScreenInch = (decimal?)d.DeviceScreen.Inch,
                    ScreenWidth = (int?)d.DeviceScreen.Width,
                    ScreenHeight = (int?)d.DeviceScreen.Height,
                    d.ScreenColor,
                    d.Firmware,
                    d.Hardware,
                    // Null-safe: StatusId 0 is used as a sentinel here
                    // (see isOnline below) and has no Status row, which an
                    // inner join would silently filter the device out on.
                    status = d.Status != null ? d.Status.Name : "Unknown",
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

    /// <summary>
    /// Finds the DeviceScreens row for a panel size, creating it when the size
    /// is new. Width + height identify the panel; Inch is stored but not matched
    /// on, because Minew sometimes omits it and requiring it would duplicate a
    /// size already in the table.
    /// </summary>
    private async Task<long?> ResolveScreenIdAsync(ScreenInfo screenInfo, string screenSizeLabel)
    {
        if (screenInfo?.Width == null || screenInfo.Height == null)
            return null;

        var screen = await _context.DeviceScreens
            .FirstOrDefaultAsync(ds => ds.Width == screenInfo.Width &&
                                       ds.Height == screenInfo.Height &&
                                       ds.ScreenTypeId == (int)ScreenType.ESL_Ink);

        if (screen == null)
        {
            screen = new DeviceScreen
            {
                Name = $"Minew {screenSizeLabel ?? $"{screenInfo.Width}x{screenInfo.Height}"}",
                Width = screenInfo.Width ?? 0,
                Height = screenInfo.Height ?? 0,
                Inch = screenInfo.Inch ?? 0,
                ScreenTypeId = (int)ScreenType.ESL_Ink,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = 0,
            };
            _context.DeviceScreens.Add(screen);
            await _context.SaveChangesAsync(); // needed to get the identity
        }

        return screen.Id;
    }

    /// <summary>
    /// Fills in screen dimensions for a store's devices from the Minew cloud.
    ///
    /// A device added locally (single add or batch import) has no screen: the
    /// panel size is only knowable from Minew's response. This pulls the store's
    /// labels and, for each one, maps the local row onto the matching
    /// DeviceScreens entry - reusing the row when those dimensions are already
    /// known and inserting one when they are not. It also repairs MinewDeviceId,
    /// which batch import sets to the MAC as a placeholder.
    /// </summary>
    /// <returns>How many local devices were updated.</returns>
    private async Task<int> BackfillDeviceScreensAsync(StoreMaster store, string eqStatus = "1,2,8,9")
    {
        if (store == null || string.IsNullOrWhiteSpace(store.MinewStoreId))
            return 0;

        var provider = _eslProviderFactory.GetProvider("Minew");
        var response = await provider.GetDevicesFromCloudAsync(store.MinewStoreId, eqStatus);

        if (response == null || response.Code != 200 || response.Items == null)
            return 0;

        var storeDevices = await _context.DeviceMaster
            .Where(d => d.StoreId == store.Id)
            .ToListAsync();

        var updated = 0;

        foreach (var cloudDevice in response.Items)
        {
            var normalisedMac = NormaliseMac(cloudDevice.Mac);
            if (normalisedMac.Length == 0) continue;

            // Every local row for this MAC, not just the first. Duplicates exist
            // in the wild (a hand-added row plus a sync-created one), and
            // updating only one left the other pointing at a stale screen.
            var matches = storeDevices
                .Where(d =>
                    (!string.IsNullOrEmpty(cloudDevice.Id) && d.MinewDeviceId == cloudDevice.Id) ||
                    NormaliseMac(d.MACAddress) == normalisedMac)
                .ToList();

            if (matches.Count == 0) continue;

            var screenIdForMac = await ResolveScreenIdAsync(cloudDevice.ScreenInfo, cloudDevice.ScreenSize);

            foreach (var local in matches)
            {
            var changed = false;

            // Batch import stores the MAC as a placeholder id - replace it with
            // the real cloud id so later syncs match directly.
            if (!string.IsNullOrEmpty(cloudDevice.Id) && local.MinewDeviceId != cloudDevice.Id)
            {
                local.MinewDeviceId = cloudDevice.Id;
                changed = true;
            }

            if (local.MACAddress != normalisedMac)
            {
                local.MACAddress = normalisedMac;
                changed = true;
            }

            if (screenIdForMac.HasValue && local.ScreenId != screenIdForMac)
            {
                local.ScreenId = screenIdForMac;
                changed = true;
            }

            if (cloudDevice.ScreenInfo?.Color != null && local.ScreenColor != cloudDevice.ScreenInfo.Color)
            {
                local.ScreenColor = cloudDevice.ScreenInfo.Color;
                changed = true;
            }

            if (changed)
            {
                local.LastSyncTime = DateTime.UtcNow;
                updated++;
            }
            }
        }

        if (updated > 0)
            await _context.SaveChangesAsync();

        return updated;
    }

    /// <summary>
    /// MACs arrive in mixed shapes (e0:00:00:00:f8:53 vs e0000000f853). Compare
    /// them in one canonical form so the same label is never treated as two.
    /// </summary>
    private static string NormaliseMac(string mac) =>
        (mac ?? string.Empty).Replace(":", "").Replace("-", "").Replace(" ", "").ToLower();

    /// <summary>
    /// Fills in missing screen dimensions for a store's devices from Minew.
    /// Use after adding devices locally, where the panel size is not yet known.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpPost("devices/refresh-screens")]
    public async Task<IActionResult> RefreshDeviceScreens([FromQuery] long storeId)
    {
        try
        {
            var store = await _context.StoreMaster.FirstOrDefaultAsync(s => s.Id == storeId);

            if (store == null)
                return BadRequest(new { message = "Store not found" });

            if (string.IsNullOrWhiteSpace(store.MinewStoreId))
                return BadRequest(new { message = "This store is not linked to Minew yet." });

            var updated = await BackfillDeviceScreensAsync(store);

            return Ok(new
            {
                message = updated > 0
                    ? $"Updated screen details for {updated} device(s)."
                    : "No devices needed a screen update.",
                updated,
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lists the merchant's dynamic goodsMap fields and flags the image-shaped
    /// ones - use this to confirm which key a template's picture is bound to.
    /// </summary>
    [HttpGet("minew/dynamic-fields")]
    public async Task<IActionResult> GetMinewDynamicFields()
    {
        try
        {
            var token = await GetToken();
            var fields = await _minewService.GetDynamicFieldsAsync(token);

            return Ok(new
            {
                total = fields.Count,
                imageLike = fields.Where(f => LooksLikeImageField(f.Id)).Select(f => f.Id).ToList(),
                fields = fields.Select(f => new { f.Id, f.Name, f.ColunmDataType }).ToList(),
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// All active ESL devices held locally. The id values here are what you pass as
    /// deviceId in eslAssignments.
    /// </summary>
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
                    // Callers need this to tell a Minew label from a Standard
                    // one; it is required when registering a device.
                    deviceType = d.DeviceType,
                    // ScreenId is optional, so this is a LEFT JOIN. Inch/Height/
                    // Width are non-nullable on DeviceScreen, and materialising a
                    // NULL into them threw "Nullable object must have a value"
                    // for every device saved without a screen.
                    screenSize = d.DeviceScreen.AspectRatio,
                    ScreenInch = (decimal?)d.DeviceScreen.Inch,
                    ScreenHeight = (int?)d.DeviceScreen.Height,
                    ScreenWidth = (int?)d.DeviceScreen.Width,
                    ScreenColor = d.ScreenColor,
                    status = d.Status,
                    battery = d.Battery,
                    lastSeen = d.LastSeen,
                    storeId = d.StoreId,
                    isOnline = d.StatusId == 0,
                })
                .ToListAsync();

            // Wrapped in the standard envelope like every other read in this API.
            // It used to return a bare array, so a client following the document
            // and reading `result` got nothing.
            return Ok(new HttpResponseData<object>
            {
                Success = true,
                Message = "Devices retrieved successfully.",
                Result = devices,
                ResponsCode = 200,
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "Failed to retrieve devices.",
                Error = ex.Message,
                ResponsCode = 500,
            });
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
    /// Full device record for a MAC address, including everything it is currently
    /// bound to (product, template, message). The MAC may be given bare
    /// (e1000005e79d) or separated (e1:00:00:05:e7:9d) - both match the same
    /// device. storeId is optional and only needed to disambiguate a MAC that has
    /// been registered in more than one store.
    /// </summary>
    [HttpGet("device/by-mac/{mac}")]
    [ProducesResponseType(typeof(HttpResponseData<DeviceDetailDto>), 200)]
    [ProducesResponseType(typeof(HttpResponseData<DeviceDetailDto>), 404)]
    [ProducesResponseType(typeof(HttpResponseData<DeviceDetailDto>), 500)]
    public async Task<IActionResult> GetDeviceByMac(string mac, [FromQuery] long? storeId = null)
    {
        var response = new HttpResponseData<DeviceDetailDto>();
        try
        {
            var device = await _deviceRepo.GetDeviceDetailByMacAsync(mac, storeId);
            if (device == null)
            {
                response.Success = false;
                response.Message = storeId.HasValue
                    ? $"Device with MAC {mac} not found in store {storeId.Value}."
                    : $"Device with MAC {mac} not found.";
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
            _logger.LogError(ex, "Error retrieving device by MAC {Mac}", mac);
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

    /// <summary>
    /// True when Minew's per-MAC result means the label was accepted. The cloud
    /// localises this string, so both the English and Chinese forms count.
    /// </summary>
    private static bool IsMinewSuccess(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var v = value.Trim();
        return v.Equals("success", StringComparison.OrdinalIgnoreCase) || v == "成功";
    }

    /// <summary>
    /// Registers a device locally. The MAC may be bare 12-hex (e1000005e79d) or
    /// separated; it must be unique within the store.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Updates a device's name, screen, network details or store.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Soft-deletes a device. Rejected while the device still has active assignments.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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


    /// <summary>
    /// Flashes a label's LED so it can be found on the shelf. Does not change what the
    /// screen displays.
    /// </summary>
    [HttpGet("devices/light-up")]
    public async Task<IActionResult> LightUpDevice([FromQuery] string mac, [FromQuery] string storeId,
    [FromQuery] int color = 1, [FromQuery] int total = 5, [FromQuery] int period = 500,
    [FromQuery] int interval = 900, [FromQuery] int brightness = 100)
    {
        try
        {
            var device = await _context.DeviceMaster.FirstOrDefaultAsync(d => d.MACAddress == mac);
            var provider = _eslProviderFactory.GetProvider(device?.DeviceType ?? "Minew");

            // Minew addresses stores by its own id. Callers are inconsistent -
            // Device Management passes the local StoreMaster id while Device
            // Templates passes the cloud id - and sending a local id got
            // "门店不存在" (store does not exist) with the label never blinking.
            var cloudStoreId = await ResolveMinewStoreIdAsync(storeId);
            if (cloudStoreId == null)
                return BadRequest(new { message = $"Unknown store '{storeId}'." });

            var result = await provider.LightUpDeviceAsync(
                mac, cloudStoreId, color, total, period, interval, brightness);

            // Minew reports failure in the body, not the HTTP status, so an
            // unchecked result made every blink look successful.
            // Via object, not dynamic: the payload is a JsonElement, and both
            // `== null` and an inferred local resolve at runtime and blow up.
            object payload = result;
            string raw = payload?.ToString() ?? string.Empty;
            if (!MinewBodySucceeded(raw, out string minewMessage))
            {
                return StatusCode(502, new
                {
                    message = $"Minew rejected the blink: {minewMessage}",
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Accepts either a local StoreMaster id or a Minew cloud id and returns the
    /// cloud id. Returns null when neither matches.
    /// </summary>
    private async Task<string> ResolveMinewStoreIdAsync(string storeId)
    {
        if (string.IsNullOrWhiteSpace(storeId)) return null;

        if (long.TryParse(storeId, out var localId))
        {
            var byLocalId = await _context.StoreMaster
                .FirstOrDefaultAsync(s => s.Id == localId);

            if (byLocalId != null)
                return string.IsNullOrWhiteSpace(byLocalId.MinewStoreId) ? null : byLocalId.MinewStoreId;
        }

        // Already a cloud id.
        var byCloudId = await _context.StoreMaster
            .FirstOrDefaultAsync(s => s.MinewStoreId == storeId);

        return byCloudId?.MinewStoreId;
    }

    /// <summary>
    /// True when a raw Minew response body carries code 200.
    /// </summary>
    private static bool MinewBodySucceeded(string rawResponse, out string message)
    {
        message = null;
        if (string.IsNullOrWhiteSpace(rawResponse)) { message = "empty response"; return false; }

        try
        {
            using var doc = JsonDocument.Parse(rawResponse);
            if (doc.RootElement.TryGetProperty("msg", out var msgEl))
                message = msgEl.GetString();

            if (doc.RootElement.TryGetProperty("code", out var codeEl))
            {
                if (codeEl.ValueKind == JsonValueKind.Number && codeEl.TryGetInt32(out var c))
                    return c == 200;
                if (codeEl.ValueKind == JsonValueKind.String && int.TryParse(codeEl.GetString(), out var cs))
                    return cs == 200;
            }

            // No code field - treat as success rather than blocking a working call.
            return true;
        }
        catch (JsonException)
        {
            message = "unreadable response";
            return false;
        }
    }

    /// <summary>
    /// Registers many labels with the Minew cloud in one call, from a list of MAC
    /// addresses.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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
            // Minew answers per MAC in the account's own language - this tenant
            // returns "成功", not "success". Matching only the English literal
            // left addedCount at 0 even when every label was added, which also
            // skipped the post-add local sync gated on addedCount > 0 below.
            var addedCount = minewResponse.Data?.Values.Count(IsMinewSuccess) ?? 0;
            var failedCount = cleanedMacs.Count - addedCount;

            // STEP 1: Wake up the successfully added devices
            var wakeResult = new BatchWakeResult();
            List<string> successfullyWokenMacs = new List<string>();
            List<string> failedToWakeMacs = new List<string>();

            if (minewResponse.Code == 200 && addedCount > 0)
            {
                try
                {
                    // Same localisation trap as addedCount above: this tenant
                    // answers "成功", so matching the English literal left
                    // successMacs empty and the wake step never ran.
                    var successMacs = minewResponse.Data
                        .Where(kv => IsMinewSuccess(kv.Value))
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

                    // Screen size is only knowable from Minew, and the rows just
                    // written have ScreenId null with the MAC standing in for
                    // MinewDeviceId. Read the store back and fill both in.
                    try
                    {
                        var screensSet = await BackfillDeviceScreensAsync(store);
                        if (screensSet > 0)
                            _logger.LogInformation("Backfilled screens for {Count} device(s)", screensSet);
                    }
                    catch (Exception screenEx)
                    {
                        // The devices are in Minew either way - do not fail the
                        // import because the follow-up read did not work.
                        _logger.LogWarning(screenEx, "Could not backfill device screens after batch add");
                    }
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

    /// <summary>
    /// Same as batch-add-minew, taking the MAC addresses from an uploaded Excel file
    /// instead of a JSON list.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Wakes sleeping labels so they accept the next bind. A label that was asleep
    /// when bound will not repaint until it wakes.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Waits delaySeconds, then re-syncs the store's devices from the cloud. Pairs with
    /// batch-wake: labels report their new state only once they have woken.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// One screen definition - physical size, resolution and colour capability.
    /// </summary>
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

    /// <summary>
    /// Paged list of screen definitions.
    /// </summary>
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

    /// <summary>
    /// Screen definitions that can be assigned to a device, optionally filtered by
    /// screen type.
    /// </summary>
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

    /// <summary>
    /// Creates a screen definition (inches, width, height, colour).
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Updates a screen definition. The id travels in the body, not the route.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Deletes a screen definition. Check can-delete first - a screen in use by any
    /// device cannot be removed.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Flips a screen definition between active and inactive. The user id travels in
    /// the body.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// How many devices use this screen definition.
    /// </summary>
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

    /// <summary>
    /// Whether this screen definition can be deleted - false when any device uses it.
    /// </summary>
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

    /// <summary>
    /// Paged list of gateways. A gateway is the radio bridge between the labels and
    /// the cloud.
    /// </summary>
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

    /// <summary>
    /// One gateway by id.
    /// </summary>
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

    /// <summary>
    /// Registers a gateway locally.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Updates a gateway's name or details.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Soft-deletes a gateway.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Pulls the store's gateways down from the Minew cloud into the local records.
    /// </summary>
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
                    // Null-safe: StatusId is a non-nullable FK, so dereferencing
                    // the navigation directly makes EF emit an INNER JOIN. Any
                    // gateway whose StatusId has no Status row was silently
                    // dropped from this list - the endpoint returned an empty
                    // array while the gateway was present and online.
                    Status = g.Status != null ? g.Status.Name : "Unknown",
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

    /// <summary>
    /// Registers a gateway with the Minew cloud so its labels can reach the network.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Minew label templates synced into SmartShelf. The id values here are what you
    /// pass as templateId - they are long numeric strings, so keep them quoted.
    /// 
    /// A template belongs to one Minew store and one screen size; using one from
    /// another store is rejected at bind time with 模板不存在.
    ///
    /// storeId is optional: omit it and every store's templates come back, as
    /// before. Supply it and the list is scoped, matching template/{id}.
    /// </summary>
    [HttpGet("template/local")]
    public async Task<IActionResult> GetLocalTemplate([FromQuery] long? storeId = null)
    {
        try
        {
            var query = _context.MinewTemplates
                .Where(d => d.IsActive);

            // Applied only when supplied - the portal calls this without a store.
            if (storeId.HasValue)
                query = query.Where(d => d.StoreId == storeId.Value);

            var templates = await query.ToListAsync();

            // Standard envelope - see devices/local above.
            return Ok(new HttpResponseData<object>
            {
                Success = true,
                Message = "Templates retrieved successfully.",
                Result = templates,
                ResponsCode = 200,
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "Failed to retrieve templates.",
                Error = ex.Message,
                ResponsCode = 500,
            });
        }
    }

    // New paginated endpoints
    /// <summary>
    /// Paged version of devices/local.
    /// </summary>
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

    /// <summary>
    /// Paged list of local templates.
    /// </summary>
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
    public async Task<IActionResult> GetTemplateById(string id, [FromQuery] long? storeId = null)
    {
        var response = new HttpResponseData<TemplateDto>();
        try
        {
            var template = await _deviceRepo.GetTempalteByIdAsync(id, storeId);
            if (template == null)
            {
                response.Success = false;
                response.Message = storeId.HasValue
                    ? $"Template with ID {id} not found in store {storeId.Value}."
                    : $"Template with ID {id} not found.";
                response.ResponsCode = 404;
                return NotFound(response);
            }

            response.Success = true;
            response.Message = "Template retrieved successfully.";
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
    /// <summary>
    /// Paged template list taking its paging and search as plain query parameters
    /// rather than a bound request object.
    /// </summary>
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

    /// <summary>
    /// Pulls the store's templates down from the Minew cloud. Run this after creating
    /// or editing a template in the Minew console.
    /// </summary>
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

    /// <summary>
    /// Renders a preview image of a template without sending anything to hardware.
    /// </summary>
    [HttpPost("templates/preview")]
    public async Task<IActionResult> GetTemplatePreview([FromBody] TemplatePreviewRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.TemplateId))
                return BadRequest(new { message = "TemplateId is required" });

            var template = await _context.MinewTemplates
                .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.IsActive);

            if (template == null)
                return NotFound(new { message = $"Template {request.TemplateId} not found." });

            // Scope the template to the store. Templates belong to one store in
            // the Minew cloud, and previewing (or later binding) one that belongs
            // elsewhere fails with "template does not exist" - better to say so
            // here than to pass it through and let the cloud answer in Chinese.
            if (!string.IsNullOrWhiteSpace(request.StoreId))
            {
                var cloudStoreId = await ResolveMinewStoreIdAsync(request.StoreId);
                var localStore = await _context.StoreMaster
                    .FirstOrDefaultAsync(x => x.MinewStoreId == cloudStoreId);

                if (localStore != null && template.StoreId != localStore.Id)
                {
                    return BadRequest(new
                    {
                        message =
                            $"Template {request.TemplateId} belongs to another store and " +
                            "cannot be previewed or bound here.",
                    });
                }
            }

            // Minew's preview endpoints key on demoName - the template NAME, not
            // its id ("Query the template preview image using the template name").
            // Passing the id returned an empty preview every time.
            var demoName = string.IsNullOrWhiteSpace(template.Name)
                ? request.TemplateId
                : template.Name;

            var provider = _eslProviderFactory.GetProvider("Minew");
            string previewData;

            if (request.isBound && !string.IsNullOrEmpty(request.Mac) && !string.IsNullOrEmpty(request.StoreId))
            {
                // Minew addresses stores by its own id. Accept either form here and
                // resolve, so a caller passing the SmartShelf store id - which is
                // what the rest of this API takes - does not silently fail.
                var previewStoreId = await ResolveMinewStoreIdAsync(request.StoreId) ?? request.StoreId;
                previewData = await provider.GetBoundTemplatePreviewAsync(demoName, request.Mac, previewStoreId);
            }
            else
            {
                previewData = await provider.GetUnboundTemplatePreviewAsync(demoName);
            }

            // Cache the preview only when it fits. PreviewImage is nvarchar(500)
            // and a rendered preview is a base64 image tens of kilobytes long, so
            // assigning it unconditionally made SaveChanges throw and turned a
            // working preview into a 500. The image is returned either way;
            // widening the column would be needed to persist it.
            const int previewColumnLimit = 500;
            if (!string.IsNullOrEmpty(previewData) && previewData.Length <= previewColumnLimit)
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

    /// <summary>
    /// Removes a local template record. Does not delete it from the Minew console.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Pairs a device with a template. The resulting combo id is what bind and
    /// assignment calls refer to. Reuses an existing pair rather than duplicating it.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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
    /// <summary>
    /// All device+template pairings.
    /// </summary>
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
                Message = "Device template combos retrieved successfully",
                ResponsCode = 200
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

            // The repository answers null for an unknown id rather than throwing.
            // Returning 200 with a null result made a missing combo indistinguishable
            // from a found one for any caller reading the status code.
            if (result == null)
            {
                return NotFound(new HttpResponseData<object>
                {
                    Success = false,
                    Message = $"DeviceTemplateCombo with ID {id} not found.",
                    ResponsCode = 404
                });
            }

            return Ok(new HttpResponseData<DeviceTemplateComboDto>
            {
                Result = result,
                Success = true,
                Message = "DeviceTemplateCombo retrieved successfully.",
                ResponsCode = 200
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message,
                ResponsCode = 404
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message,
                ResponsCode = 409
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving DeviceTemplateCombo with ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while retrieving DeviceTemplateCombo",
                Error = ex.Message,
                ResponsCode = 500
            });
        }
    }

    /// <summary>
    /// Update an existing DeviceTemplateCombo
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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
                    Error = ModelState.Values.ToString(),
                    ResponsCode = 400
                });
            }

            var result = await _deviceRepo.UpdateDeviceTemplateCombosAsync(id, dto);

            return Ok(new HttpResponseData<DeviceTemplateCombos>
            {
                Result = result,
                Success = true,
                Message = "DeviceTemplateCombo updated successfully",
                ResponsCode = 200
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message,
                ResponsCode = 404
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message,
                ResponsCode = 409
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DeviceTemplateCombo with ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while updating DeviceTemplateCombo",
                Error = ex.Message,
                ResponsCode = 500
            });
        }
    }

    // POST combos/{id} was a byte-for-byte duplicate of the PUT above - same DTO,
    // same repository call - and nothing called it. Removed so the update path has
    // one route, and one place to keep correct.

    /// <summary>
    /// Deletes a device+template pairing. Rejected while an active assignment uses it.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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
                // Still used by an active assignment. This was returned as 200 with
                // success:false, so a caller reading only the status code recorded a
                // refusal as a successful delete. It is a state conflict - 409, in
                // line with the queue endpoints.
                return Conflict(new HttpResponseData<bool>
                {
                    Success = false,
                    Message = message,
                    Result = false,
                    ResponsCode = 409
                });
            }

            return Ok(new HttpResponseData<bool>
            {
                Success = true,
                Message = message,
                Result = true,
                ResponsCode = 200
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Combo not found for deletion");
            return NotFound(new HttpResponseData<object>
            {
                Success = false,
                Message = ex.Message,
                ResponsCode = 404
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting combo ID: {Id}", id);
            return StatusCode(500, new HttpResponseData<object>
            {
                Success = false,
                Message = "An error occurred while deleting the combo",
                Error = ex.Message,
                ResponsCode = 500
            });
        }
    }

    #endregion

    // The device-message combination endpoints were removed: assigning standalone
    // promotional messages to a device is not part of the Minew-only scope. The
    // DeviceMessageCombos model, table and repository methods are intentionally left
    // in place - DeviceAssignment still carries a 'MESSAGE' assignment type, and a
    // Minew template can still render a message image via goodsMap["image"].


    #region Combination Assignments(to shelf or product) Handlers 
    // ============ ASSIGNMENTS (Using DeviceTemplateAssignment Table) ============
    /// <summary>
    /// Attaches a combo to a product, shelf or aisle. Creating a product with
    /// eslAssignments does this for you - this endpoint is for attaching to a shelf or
    /// aisle, or repairing an assignment by hand.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Paged list of assignments across all locations.
    /// </summary>
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

    /// <summary>
    /// Assignments for one location. LocationType is Product, Shelf or Aisle.
    /// </summary>
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


    /// <summary>
    /// Changes the display order when several labels serve the same location.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Removes an assignment and unbinds the label in the vendor cloud, so it stops
    /// showing stale data. A cloud failure is logged but does not block the local removal.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Paged version of the per-location assignment list.
    /// </summary>
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

    /// <summary>
    /// Paged device list with filtering and search.
    /// </summary>
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

    /// <summary>
    /// Paged template list with filtering and search.
    /// </summary>
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

    /// <summary>
    /// Renders what a label would show for a given template and product without sending
    /// anything to the hardware. Useful for checking a template before binding.
    /// </summary>
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

    /// <summary>
    /// Forces a label to re-render a product it is already assigned to.
    /// 
    /// Not normally needed - creating or updating a product with ESL assignments binds the
    /// label automatically. Use this to repaint a label that was offline at bind time, or
    /// after changing a template outside the product flow.
    /// 
    /// ComboId is the DeviceTemplateComboId from GET /api/products/by-code/{productCode}.
    /// Supply either ProductId (the data is built from the product) or an explicit GoodsMap.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

            // The cloud keys on its own store id, not ours. This sent the local
            // StoreId (e.g. 3), so every call came back 门店不存在 - "store does not
            // exist" - and this endpoint never bound anything.
            var bindStore = await _context.StoreMaster
                .FirstOrDefaultAsync(s => s.Id == combo.Device.StoreId);

            if (bindStore == null || string.IsNullOrEmpty(bindStore.MinewStoreId))
                return BadRequest(new { message = "Store has no MinewStoreId - the label cannot be bound in the cloud." });

            // Prepare bind request for the vendor's cloud API
            var bindRequest = new
            {
                storeId = bindStore.MinewStoreId,
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

    /// <summary>
    /// Binds a label from either a template combo or a message combo.
    /// 
    /// A message combo carries a device and a message but no template, and a Minew bind
    /// cannot render without one - so this resolves the device's own template combo and
    /// uses the message as the image.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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
  

    /// <summary>
    /// Binds a shelf-level message to a label, rendering the message image over the
    /// combo's template.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Binds a product to a label using the product's own data - a thin wrapper over
    /// POST /api/device/bind that builds the goods map for you.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Binds many labels in one call, reporting success or failure per row rather than
    /// aborting the batch on the first problem.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Whether a dynamic field id looks like an image slot. Minew names these
    /// per merchant - IMAGE001, base64, ICON1 and so on - so match on shape
    /// rather than assuming one universal key.
    /// </summary>
    private static bool LooksLikeImageField(string fieldId)
    {
        if (string.IsNullOrWhiteSpace(fieldId)) return false;
        var id = fieldId.Trim().ToLowerInvariant();
        return id.Contains("image") || id.Contains("img") ||
               id.Contains("base64") || id.Contains("icon") || id.Contains("pic");
    }

    /// <summary>
    /// The goodsMap keys a picture should be sent under. Discovered from the
    /// merchant's dynamic fields; falls back to "image" when the lookup fails so
    /// binding never breaks just because this call did.
    /// </summary>
    private async Task<List<string>> ResolveImageFieldKeysAsync()
    {
        try
        {
            var token = await GetToken();
            var fields = await _minewService.GetDynamicFieldsAsync(token);

            var keys = fields
                .Select(f => f.Id)
                .Where(LooksLikeImageField)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Minew ignores keys a template does not use, so sending the picture
            // under every image-shaped field is safe and means the one the
            // template is actually bound to is always covered.
            if (!keys.Any(k => string.Equals(k, "image", StringComparison.OrdinalIgnoreCase)))
                keys.Add("image");

            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read Minew dynamic fields; falling back to the 'image' key");
            return new List<string> { "image" };
        }
    }

    /// <summary>
    /// Minew wants the picture as a bare base64 string ("the data is base64
    /// string format"). Messages are stored as data URIs
    /// ("data:image/jpeg;base64,...."), and sending that whole string meant the
    /// cloud accepted the bind but could not decode the picture - a label that
    /// rendered the template with no image.
    /// </summary>
    private static string ToBareBase64(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return content;

        if (content.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var marker = content.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
            if (marker > 0) return content[(marker + 7)..].Trim();
        }

        return content.Trim();
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







