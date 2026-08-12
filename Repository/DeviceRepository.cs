using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Data;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Interface;
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Models.shelf;
using TERMS_LOYALTY_API.Models.user;
using TERMS_LOYALTY_API.Services;
using TERMS_LOYALTY_API.Shared.Enum;
using TERMS_LOYALTY_API.Shared.Helpers;
using static TERMS_LOYALTY_API.DTOs.shelf.AssignmentDto;

namespace TERMS_LOYALTY_API.Repository
{
    public class DeviceRepository : IDevice
    {
        private readonly SmartShelfDbContext _context;
        private readonly ILogger<DeviceRepository> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly MinewCloudService _minewService;

        public DeviceRepository(SmartShelfDbContext context, ILogger<DeviceRepository> logger, IHttpContextAccessor httpContextAccessor, MinewCloudService minewService)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _minewService = minewService;
        }

        #region Device Methods

        //public async Task<PagedResult<DeviceDto>> GetDevicesPagedAsync(DevicePagedRequest request)
        //{
        //    try
        //    {
        //        // Start with base query
        //        var query = _context.DeviceMaster
        //            .Include(d => d.DeviceScreen)
        //            .Include(d => d.Store)
        //            .Include(d => d.Status)
        //            .Where(d => d.IsActive)
        //            .AsQueryable();

        //        // Apply store filter (most important as you mentioned most devices are from store)
        //        if (request.StoreId.HasValue && request.StoreId.Value > 0)
        //        {
        //            query = query.Where(d => d.StoreId == request.StoreId.Value);
        //        }

        //        // Apply other filters
        //        if (request.IsOnline.HasValue)
        //        {
        //            query = query.Where(d => d.StatusId == (request.IsOnline.Value ? 0 : 1));
        //        }

        //        if (!string.IsNullOrWhiteSpace(request.Status))
        //        {
        //            query = query.Where(d => d.Status != null && d.Status.Name == request.Status);
        //        }

        //        if (request.MinBattery.HasValue)
        //        {
        //            query = query.Where(d => d.Battery >= request.MinBattery.Value);
        //        }

        //        if (request.MaxBattery.HasValue)
        //        {
        //            query = query.Where(d => d.Battery <= request.MaxBattery.Value);
        //        }

        //        if (request.LastSeenFrom.HasValue)
        //        {
        //            query = query.Where(d => d.LastSeen >= request.LastSeenFrom.Value);
        //        }

        //        if (request.LastSeenTo.HasValue)
        //        {
        //            query = query.Where(d => d.LastSeen <= request.LastSeenTo.Value);
        //        }

        //        // Apply search term
        //        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        //        {
        //            var searchTerm = request.SearchTerm.Trim().ToLower();
        //            query = query.Where(d =>
        //                (d.Name != null && d.Name.ToLower().Contains(searchTerm)) ||
        //                (d.MACAddress != null && d.MACAddress.ToLower().Contains(searchTerm)) ||
        //                (d.NetworkName != null && d.NetworkName.ToLower().Contains(searchTerm)) ||
        //                (d.IPAddress != null && d.IPAddress.Contains(searchTerm)) ||
        //                (d.Store != null && d.Store.StoreName != null && d.Store.StoreName.ToLower().Contains(searchTerm))
        //            );
        //        }

        //        // Get total count
        //        var totalCount = await query.CountAsync();

        //        // For SQL Server 2008 compatibility, fetch all matching records and paginate in memory
        //        var allDevices = await query.ToListAsync();

        //        // Apply sorting in memory
        //        var sortedDevices = ApplyDeviceSorting(allDevices.AsQueryable(), request.SortBy, request.SortDescending);

        //        // Apply pagination
        //        var pagedDevices = sortedDevices
        //            .Skip((request.PageNumber - 1) * request.PageSize)
        //            .Take(request.PageSize)
        //            .ToList();

        //        // Map to DTO
        //        var deviceDtos = pagedDevices.Select(MapToDto).ToList();

        //        return new PagedResult<DeviceDto>
        //        {
        //            Items = deviceDtos,
        //            TotalCount = totalCount,
        //            PageNumber = request.PageNumber,
        //            PageSize = request.PageSize,
        //            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
        //            SearchTerm = request.SearchTerm
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"Error retrieving paginated devices: {ex.Message}", ex);
        //    }
        //}

        public async Task<PagedResult<DeviceDto>> GetDevicesPagedAsync(DevicePagedRequest request)
        {
            try
            {
                var query =
                    from d in _context.DeviceMaster
                    where d.IsActive

                    // LEFT JOIN Store
                    join s in _context.StoreMaster
                        on d.StoreId equals s.Id into storeJoin
                    from s in storeJoin.DefaultIfEmpty()

                        // LEFT JOIN Status
                    join st in _context.Status
                        on d.StatusId equals st.Id into statusJoin
                    from st in statusJoin.DefaultIfEmpty()

                        // LEFT JOIN Screen
                    join ds in _context.DeviceScreens
                        on d.ScreenId equals ds.Id into screenJoin
                    from ds in screenJoin.DefaultIfEmpty()

                        // LEFT JOIN Created User
                    join cu in _context.Users
                        on d.CreatedUser equals cu.Id into cuJoin
                    from cu in cuJoin.DefaultIfEmpty()

                        // LEFT JOIN Updated User
                    join uu in _context.Users
                        on d.UpdatedUser equals uu.Id into uuJoin
                    from uu in uuJoin.DefaultIfEmpty()

                    select new
                    {
                        Device = d,
                        StoreName = s != null ? s.StoreName : string.Empty,
                        StatusName = st != null ? st.Name : "Unknown",
                        Screen = ds,
                        CreatedUserName = cu != null ? cu.UserName : string.Empty,
                        UpdatedUserName = uu != null ? uu.UserName : string.Empty
                    };

                // ================= FILTERS =================

                if (request.StoreId.HasValue && request.StoreId.Value > 0)
                {
                    query = query.Where(x => x.Device.StoreId == request.StoreId.Value);
                }

                if (request.BrandId.HasValue && request.BrandId.Value > 0)
                {
                    // No FK from DeviceMaster to EslBrandMaster - DeviceType is a free-text
                    // string that matches EslBrandMaster.Code, so resolve the code first.
                    var brandCode = await _context.EslBrandMaster
                        .Where(b => b.Id == request.BrandId.Value)
                        .Select(b => b.Code)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrWhiteSpace(brandCode))
                    {
                        query = query.Where(x => x.Device.DeviceType == brandCode);
                    }
                }

                if (request.IsOnline.HasValue)
                {
                    query = query.Where(x =>
                        x.Device.StatusId == (request.IsOnline.Value ? 0 : 1));
                }

                if (!string.IsNullOrWhiteSpace(request.Status))
                {
                    query = query.Where(x => x.StatusName.Trim() == request.Status.Trim().ToUpper());
                }

                if (request.MinBattery.HasValue)
                {
                    query = query.Where(x => x.Device.Battery >= request.MinBattery.Value);
                }

                if (request.MaxBattery.HasValue)
                {
                    query = query.Where(x => x.Device.Battery <= request.MaxBattery.Value);
                }

                if (request.LastSeenFrom.HasValue)
                {
                    query = query.Where(x => x.Device.LastSeen >= request.LastSeenFrom.Value);
                }

                if (request.LastSeenTo.HasValue)
                {
                    query = query.Where(x => x.Device.LastSeen <= request.LastSeenTo.Value);
                }

                // ================= SEARCH =================

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var term = request.SearchTerm.Trim().ToLower();

                    query = query.Where(x =>
                        (x.Device.Name != null && x.Device.Name.ToLower().Contains(term)) ||
                        (x.Device.MACAddress != null && x.Device.MACAddress.ToLower().Contains(term)) ||
                        (x.Device.NetworkName != null && x.Device.NetworkName.ToLower().Contains(term)) ||
                        (x.Device.IPAddress != null && x.Device.IPAddress.Contains(term)) ||
                        (x.StoreName != null && x.StoreName.ToLower().Contains(term))
                    );
                }

                // ================= COUNT =================

                var totalCount = await query.CountAsync();

                // ================= SQL 2008 SAFE =================

                var rows = await query.ToListAsync();

                // ================= SORT =================

                var sorted = ApplyDeviceSorting(
                    rows.Select(x => x.Device).AsQueryable(),
                    request.SortBy,
                    request.SortDescending);

                // ================= PAGE =================

                var paged = sorted
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // ================= MAP =================

                var deviceDtos = paged
                    .Join(rows,
                        d => d.Id,
                        x => x.Device.Id,
                        (d, x) => new DeviceDto
                        {
                            Id = d.Id,
                            Mac = d.MACAddress ?? string.Empty,
                            DeviceName = d.Name ?? string.Empty,

                            ScreenId = x.Screen?.Id,
                            ScreenInch = x.Screen?.Inch,
                            ScreenHeight = x.Screen?.Height,
                            ScreenWidth = x.Screen?.Width,
                            ScreenColor = d.ScreenColor ?? string.Empty,

                            Status = x.StatusName,
                            Battery = d.Battery,
                            LastSeen = d.LastSeen,

                            StoreId = d.StoreId,
                            StoreName = x.StoreName,

                            IsOnline = d.StatusId == 0,
                            NetworkName = d.NetworkName ?? string.Empty,
                            IPAddress = d.IPAddress ?? string.Empty,
                            Firmware = d.Firmware ?? string.Empty,
                            Hardware = d.Hardware ?? string.Empty,
                            DeviceType = d.DeviceType ?? string.Empty,

                            CreatedDate = d.CreatedDate,
                            CreatedUser = x.CreatedUserName,
                            UpdatedUser = x.UpdatedUserName
                        })
                    .ToList();

                return new PagedResult<DeviceDto>
                {
                    Items = deviceDtos,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
                    SearchTerm = request.SearchTerm
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving paginated devices: {ex.Message}", ex);
            }
        }


        public async Task<DeviceDto> CreateDeviceAsync(CreateDeviceRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Validate store exists
                var store = await _context.StoreMaster
                    .FirstOrDefaultAsync(s => s.Id == request.StoreId && s.IsActive);

                if (store == null)
                    throw new ArgumentException($"Store with ID {request.StoreId} not found");

                // Validate screen if provided
                if (request.ScreenId.HasValue)
                {
                    var screen = await _context.DeviceScreens
                        .FirstOrDefaultAsync(s => s.Id == request.ScreenId.Value && s.IsActive);

                    if (screen == null)
                        throw new ArgumentException($"Screen with ID {request.ScreenId} not found");
                }

                // Check for duplicate MAC in same store
                var existingDevice = await _context.DeviceMaster
                    .FirstOrDefaultAsync(d => d.MACAddress == request.MacAddress && d.StoreId == request.StoreId);

                if (existingDevice != null)
                    throw new InvalidOperationException($"Device with MAC {request.MacAddress} already exists in store {store.StoreName}");

                // Create new device
                var device = new DeviceMaster
                {
                    MACAddress = request.MacAddress,
                    Name = request.Name,
                    Description = $"{request.DeviceType} Device",
                    StatusId = request.StatusId ?? (int)DeviceStatus.Active,
                    DeviceType = request.DeviceType,
                    StoreId = request.StoreId,
                    ScreenId = request.ScreenId,
                    IPAddress = request.IpAddress ?? "",
                    NetworkName = request.NetworkName ?? "",
                    Firmware = request.Firmware ?? "",
                    Hardware = request.Hardware ?? "",
                    Battery = request.Battery ?? 100,
                    IsActive = request.IsActive ?? true,
                    IsOnline = false,
                    LastSeen = DateTime.UtcNow,
                    LastSyncTime = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = request.CreatedUser ?? 0
                };

                // Set MinewDeviceId for Minew devices
                if (request.DeviceType == "Minew")
                {
                    device.MinewDeviceId = request.MacAddress;
                }

                _context.DeviceMaster.Add(device);
                await _context.SaveChangesAsync();

                // Map to DTO
                var deviceDto = await GetDeviceByIdAsync(device.Id);

                await transaction.CommitAsync();
                return deviceDto;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating device: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<DeviceDto> UpdateDeviceAsync(long id, UpdateDeviceRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var device = await _context.DeviceMaster
                    .Include(d => d.Status)
                    .Include(d => d.Store)
                    .Include(d => d.DeviceScreen)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (device == null)
                    throw new KeyNotFoundException($"Device with ID {id} not found");

                // Validate store if changed
                if (request.StoreId.HasValue && request.StoreId != device.StoreId)
                {
                    var store = await _context.StoreMaster
                        .FirstOrDefaultAsync(s => s.Id == request.StoreId.Value && s.IsActive);

                    if (store == null)
                        throw new ArgumentException($"Store with ID {request.StoreId} not found");

                    // Check for duplicate MAC in new store
                    var existingMac = await _context.DeviceMaster
                        .FirstOrDefaultAsync(d => d.MACAddress == (request.MacAddress ?? device.MACAddress)
                            && d.StoreId == request.StoreId.Value
                            && d.Id != id);

                    if (existingMac != null)
                        throw new InvalidOperationException($"Device with MAC {request.MacAddress ?? device.MACAddress} already exists in store {store.StoreName}");
                }

                // Validate screen if changed
                if (request.ScreenId.HasValue && request.ScreenId != device.ScreenId)
                {
                    var screen = await _context.DeviceScreens
                        .FirstOrDefaultAsync(s => s.Id == request.ScreenId.Value && s.IsActive);

                    if (screen == null)
                        throw new ArgumentException($"Screen with ID {request.ScreenId} not found");
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(request.MacAddress))
                    device.MACAddress = request.MacAddress;

                if (!string.IsNullOrEmpty(request.Name))
                    device.Name = request.Name;

                if (!string.IsNullOrEmpty(request.DeviceType))
                    device.DeviceType = request.DeviceType;

                if (request.StoreId.HasValue)
                    device.StoreId = request.StoreId.Value;

                if (request.ScreenId.HasValue)
                    device.ScreenId = request.ScreenId;

                if (request.IpAddress != null)
                    device.IPAddress = request.IpAddress;

                if (request.NetworkName != null)
                    device.NetworkName = request.NetworkName;

                if (request.Firmware != null)
                    device.Firmware = request.Firmware;

                if (request.Hardware != null)
                    device.Hardware = request.Hardware;

                if (request.Battery.HasValue)
                    device.Battery = request.Battery.Value;

                if (request.StatusId.HasValue)
                    device.StatusId = request.StatusId.Value;

                if (request.IsActive.HasValue)
                    device.IsActive = request.IsActive.Value;

                // Update MinewDeviceId for Minew devices
                if (request.DeviceType == "Minew" && !string.IsNullOrEmpty(request.MacAddress))
                {
                    device.MinewDeviceId = request.MacAddress;
                }

                device.UpdatedDate = DateTime.UtcNow;
                device.UpdatedUser = request.UpdatedUser ?? 0;

                await _context.SaveChangesAsync();

                // Map to DTO
                var deviceDto = await GetDeviceByIdAsync(device.Id);

                await transaction.CommitAsync();
                return deviceDto;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating device ID {Id}: {Message}", id, ex.Message);
                throw;
            }
        }

        public async Task<(bool success, string message)> DeleteDeviceAsync(long id, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Check if device has active template combos
                var hasTemplateCombo = await _context.DeviceTemplateCombos
                    .AnyAsync(x => x.DeviceId == id && x.IsActive);

                if (hasTemplateCombo)
                {
                    return (false, "Cannot delete device: it is used in active template combos.");
                }

                // Check if device has active message combos
                var hasMessageCombo = await _context.DeviceMessageCombos
                    .AnyAsync(x => x.DeviceId == id && x.IsActive);

                if (hasMessageCombo)
                {
                    return (false, "Cannot delete device: it is used in active message combos.");
                }

                // Fetch device
                var device = await _context.DeviceMaster.FirstOrDefaultAsync(d => d.Id == id);
                if (device == null)
                    throw new KeyNotFoundException($"Device with ID {id} not found");

                // Soft delete
                device.IsActive = false;
                device.UpdatedDate = DateTime.UtcNow;
                device.UpdatedUser = userId;
                device.StatusId = (int)DeviceStatus.Inactive;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Device deleted successfully");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting device ID {Id}: {Message}", id, ex.Message);
                throw;
            }
        }

        public async Task<List<DeviceDto>> GetDevicesByIdsAsync(List<long> ids)
        {
            return await _context.DeviceMaster
                .Where(d => ids.Contains(d.Id))
                .Select(d => new DeviceDto
                {
                    Id = d.Id,
                    Mac = d.MACAddress ?? string.Empty,
                    DeviceName = d.Name ?? string.Empty,

                    //ScreenId = x.Screen?.Id,
                    //ScreenInch = x.Screen?.Inch,
                    //ScreenHeight = x.Screen?.Height,
                    //ScreenWidth = x.Screen?.Width,
                    ScreenColor = d.ScreenColor ?? string.Empty,

                    //Status = x.StatusName,
                    Battery = d.Battery,
                    LastSeen = d.LastSeen,

                    StoreId = d.StoreId,
                    //StoreName = x.StoreName,

                    IsOnline = d.StatusId == 0,
                    NetworkName = d.NetworkName ?? string.Empty,
                    IPAddress = d.IPAddress ?? string.Empty,
                    Firmware = d.Firmware ?? string.Empty,
                    Hardware = d.Hardware ?? string.Empty,
                    DeviceType = d.DeviceType ?? string.Empty,

                    CreatedDate = d.CreatedDate,
                    //CreatedUser = x.CreatedUserName,
                    //UpdatedUser = x.UpdatedUserName
                })
                .ToListAsync();
        }

        public async Task<List<EslBrandDto>> GetActiveBrandsAsync()
        {
            return await _context.EslBrandMaster
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new EslBrandDto
                {
                    Id = b.Id,
                    Code = b.Code,
                    Name = b.Name,
                    IsActive = b.IsActive
                })
                .ToListAsync();
        }

        private IQueryable<DeviceMaster> ApplyDeviceSorting(IQueryable<DeviceMaster> query, string? sortBy, bool sortDescending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                // Default sorting by LastSeen (descending) then by Name
                return query.OrderByDescending(d => d.LastSeen)
                           .ThenBy(d => d.Name);
            }

            return sortBy.ToLower() switch
            {
                "name" or "devicename" => sortDescending
                    ? query.OrderByDescending(d => d.Name)
                    : query.OrderBy(d => d.Name),
                "macaddress" or "mac" => sortDescending
                    ? query.OrderByDescending(d => d.MACAddress)
                    : query.OrderBy(d => d.MACAddress),
                "lastseen" => sortDescending
                    ? query.OrderByDescending(d => d.LastSeen)
                    : query.OrderBy(d => d.LastSeen),
                "battery" => sortDescending
                    ? query.OrderByDescending(d => d.Battery)
                    : query.OrderBy(d => d.Battery),
                "storename" => sortDescending
                    ? query.OrderByDescending(d => d.Store != null ? d.Store.StoreName : "")
                    : query.OrderBy(d => d.Store != null ? d.Store.StoreName : ""),
                "status" => sortDescending
                    ? query.OrderByDescending(d => d.Status != null ? d.Status.Name : "")
                    : query.OrderBy(d => d.Status != null ? d.Status.Name : ""),
                "networkname" => sortDescending
                    ? query.OrderByDescending(d => d.NetworkName)
                    : query.OrderBy(d => d.NetworkName),
                "ipaddress" => sortDescending
                    ? query.OrderByDescending(d => d.IPAddress)
                    : query.OrderBy(d => d.IPAddress),
                _ => query.OrderByDescending(d => d.LastSeen)
                         .ThenBy(d => d.Name)
            };
        }

        #endregion
        public async Task<PagedResult<TemplateDto>> GetTemplatesPagedAsync(TemplatePagedRequest request)
        {
            try
            {
                // Start with base query
                var query = _context.MinewTemplates
                    .Where(t => t.IsActive)
                    .AsQueryable();

                // Apply filters
                if (request.StoreId != null)
                {
                    query = query.Where(t => t.StoreId == request.StoreId);
                }

                if (request.IsActive.HasValue)
                {
                    query = query.Where(t => t.IsActive == request.IsActive.Value);
                }

                if (request.ScreenSize.HasValue)
                {
                    query = query.Where(t => t.ScreenInch == request.ScreenSize.Value);
                }

                if (!string.IsNullOrWhiteSpace(request.Color))
                {
                    query = query.Where(t => t.Color == request.Color);
                }

                if (request.Orientation.HasValue)
                {
                    query = query.Where(t => t.Orientation == request.Orientation.Value);
                }

                // Apply search term
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.Trim().ToLower();
                    query = query.Where(t =>
                        (t.Name != null && t.Name.ToLower().Contains(searchTerm)) ||
                        (t.Description != null && t.Description.ToLower().Contains(searchTerm)) ||
                        (t.Color != null && t.Color.ToLower().Contains(searchTerm))
                    );
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // For SQL Server 2008 compatibility, fetch all matching records and paginate in memory
                var allTemplates = await query.ToListAsync();

                // Apply sorting in memory
                var sortedTemplates = ApplyTemplateSorting(allTemplates.AsQueryable(), request.SortBy, request.SortDescending);

                // Apply pagination
                var pagedTemplates = sortedTemplates
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // Map to DTO
                var templateDtos = pagedTemplates.Select(MapTemplateToDto).ToList();

                return new PagedResult<TemplateDto>
                {
                    Items = templateDtos,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
                    SearchTerm = request.SearchTerm
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving paginated templates: {ex.Message}", ex);
            }
        }


        private IQueryable<MinewTemplates> ApplyTemplateSorting(IQueryable<MinewTemplates> query, string? sortBy, bool sortDescending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                // Default sorting by CreatedDate (descending) then by Name
                return query.OrderByDescending(t => t.CreatedDate)
                           .ThenBy(t => t.Name);
            }

            return sortBy.ToLower() switch
            {
                "name" => sortDescending
                    ? query.OrderByDescending(t => t.Name)
                    : query.OrderBy(t => t.Name),
                "createddate" => sortDescending
                    ? query.OrderByDescending(t => t.CreatedDate)
                    : query.OrderBy(t => t.CreatedDate),
                "syncdate" => sortDescending
                    ? query.OrderByDescending(t => t.SyncDate)
                    : query.OrderBy(t => t.SyncDate),
                "screeninch" or "screensize" => sortDescending
                    ? query.OrderByDescending(t => t.ScreenInch)
                    : query.OrderBy(t => t.ScreenInch),
                "color" => sortDescending
                    ? query.OrderByDescending(t => t.Color)
                    : query.OrderBy(t => t.Color),
                "orientation" => sortDescending
                    ? query.OrderByDescending(t => t.Orientation)
                    : query.OrderBy(t => t.Orientation),
                _ => query.OrderByDescending(t => t.CreatedDate)
                         .ThenBy(t => t.Name)
            };
        }

        // For backward compatibility - keep your original methods
        public async Task<List<DeviceDto>> GetDevicesAsync()
        {
            try
            {
                var devices = await _context.DeviceMaster
                    .Include(d => d.DeviceScreen)
                    .Include(d => d.Store)
                    .Include(d => d.Status)
                    .Where(d => d.IsActive)
                    .Select(d => new DeviceDto
                    {
                        Id = d.Id,
                        Mac = d.MACAddress ?? string.Empty,
                        DeviceName = d.Name ?? string.Empty,
                        ScreenSize = d.DeviceScreen.DPI,
                        ScreenInch = d.DeviceScreen.Inch,
                        ScreenHeight = d.DeviceScreen.Height,
                        ScreenWidth = d.DeviceScreen.Width,
                        ScreenColor = d.ScreenColor ?? string.Empty,
                        Status = d.Status != null ? d.Status.Name : "Unknown",
                        Battery = d.Battery,
                        LastSeen = d.LastSeen,
                        StoreId = d.StoreId,
                        IsOnline = d.StatusId == 0,
                        StoreName = d.Store != null ? d.Store.StoreName : string.Empty,
                        NetworkName = d.NetworkName ?? string.Empty,
                        IPAddress = d.IPAddress ?? string.Empty,
                        Firmware = d.Firmware ?? string.Empty,
                        Hardware = d.Hardware ?? string.Empty,
                        DeviceType = d.DeviceType ?? string.Empty
                    })
                    .ToListAsync();

                return devices;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving devices: {ex.Message}", ex);
            }
        }

        public async Task<List<TemplateDto>> GetTemplatesAsync()
        {
            try
            {
                var templates = await _context.MinewTemplates
                    .Where(t => t.IsActive)
                    .Select(t => new TemplateDto
                    {
                        Id = t.Id,
                        Name = t.Name ?? string.Empty,
                        Description = t.Description ?? string.Empty,
                        ScreenInch = t.ScreenInch,
                        ScreenWidth = t.ScreenWidth,
                        ScreenHeight = t.ScreenHeight,
                        Color = t.Color ?? string.Empty,
                        Orientation = t.Orientation,
                        PreviewImage = t.PreviewImage ?? string.Empty,
                        StoreId = t.StoreId,
                        IsActive = t.IsActive,
                        SyncDate = t.SyncDate,
                        CreatedDate = t.CreatedDate
                    })
                    .ToListAsync();

                return templates;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving templates: {ex.Message}", ex);
            }
        }

        public async Task<(bool success, string message)> DeleteTemplateAsync(string id, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Check if device has active template combos
                var hasTemplateCombo = await _context.DeviceTemplateCombos
                    .AnyAsync(x => x.TemplateId.Trim() == id.Trim() && x.IsActive);

                if (hasTemplateCombo)
                {
                    return (false, "Cannot delete template: it is used in active template combos.");
                }

              
                // Fetch template
                var template = await _context.MinewTemplates.FirstOrDefaultAsync(d => d.Id == id.Trim());
                if (template == null)
                    throw new KeyNotFoundException($"Template with ID {id} not found");

                // Soft delete
                template.IsActive = false;
                //template.UpdatedDate = DateTime.UtcNow;
                //template.UpdatedUser = userId;
                //template.StatusId = (int)DeviceStatus.Inactive;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Template deleted successfully");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting template ID {Id}: {Message}", id, ex.Message);
                throw;
            }
        }

        public async Task<List<TemplateDto>> GetTemplatesByIdsAsync(List<string> ids)
        {
            return await _context.MinewTemplates
                .Where(t => ids.Contains(t.Id))
                .Select(t => new TemplateDto
                {
                    Id = t.Id,
                    Name = t.Name ?? string.Empty,
                    Description = t.Description ?? string.Empty,
                    ScreenInch = t.ScreenInch,
                    ScreenWidth = t.ScreenWidth,
                    ScreenHeight = t.ScreenHeight,
                    Color = t.Color ?? string.Empty,
                    Orientation = t.Orientation,
                    PreviewImage = t.PreviewImage ?? string.Empty,
                    StoreId = t.StoreId,
                    IsActive = t.IsActive,
                    SyncDate = t.SyncDate,
                    CreatedDate = t.CreatedDate
                })
                .ToListAsync();
        }


        public async Task<PagedResult<DeviceTemplateComboDto>> GetCombosPagedAsync(DeviceTemplateComboPagedRequest request)
        {
            try
            {
                // Start with base query and map to ViewModel immediately
                var query = _context.DeviceTemplateCombos
                    .Join(_context.DeviceMaster,
                        combo => combo.DeviceId,
                        device => device.Id,
                        (combo, device) => new { combo, device })
                    .Join(_context.MinewTemplates,
                        x => x.combo.TemplateId,
                        template => template.Id,
                        (x, template) => new DeviceTemplateComboViewModel
                        {
                            Id = x.combo.Id,
                            DeviceId = x.combo.DeviceId,
                            DeviceMac = x.device.MACAddress,
                            TemplateId = x.combo.TemplateId,
                            DeviceName = x.device.Name,
                            TemplateName = template.Name,
                            ScreenWidth = template.ScreenWidth,
                            ScreenHeight = template.ScreenHeight,
                            ScreenInch = template.ScreenInch,
                            Battery = x.device.Battery,
                            IsDefault = x.combo.IsDefault,
                            Priority = x.combo.Priority,
                            CreatedDate = x.combo.CreatedDate,
                            IsActive = x.combo.IsActive
                        })
                    .AsQueryable();

                // Apply filters
                if (request.DeviceId.HasValue)
                {
                    query = query.Where(x => x.DeviceId == request.DeviceId.Value);
                }

                if (!string.IsNullOrEmpty(request.TemplateId))
                {
                    query = query.Where(x => x.TemplateId == request.TemplateId);
                }

                if (request.IsDefault.HasValue)
                {
                    query = query.Where(x => x.IsDefault == request.IsDefault.Value);
                }

                if (request.IsActive.HasValue)
                {
                    query = query.Where(x => x.IsActive == request.IsActive.Value);
                }

                // Apply search term
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.Trim().ToLower();
                    query = query.Where(x =>
                        (x.DeviceName != null && x.DeviceName.ToLower().Contains(searchTerm)) ||
                        (x.TemplateName != null && x.TemplateName.ToLower().Contains(searchTerm))
                    );
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // FOR SQL SERVER 2008: Fetch ALL matching records
                var allCombos = await query.ToListAsync();

                // Apply sorting in memory
                var sortedCombos = allCombos.AsQueryable()
                    .ApplyComboSorting(request.SortBy, request.SortDescending);

                // Apply pagination in memory
                var pagedCombos = sortedCombos
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // Map to DTO
                var comboDtos = pagedCombos.Select(x => new DeviceTemplateComboDto
                {
                    Id = x.Id,
                    DeviceId = x.DeviceId,
                    TemplateId = x.TemplateId,
                    DeviceName = x.DeviceName,
                    DeviceMac = x.DeviceMac,
                    TemplateName = x.TemplateName,
                    ScreenWidth = x.ScreenWidth,
                    ScreenHeight = x.ScreenHeight,
                    ScreenInch = x.ScreenInch,
                    Battery = x.Battery,
                    IsDefault = x.IsDefault,
                    Priority = x.Priority,
                    CreatedDate = x.CreatedDate,
                    IsActive = x.IsActive
                }).ToList();

                return new PagedResult<DeviceTemplateComboDto>
                {
                    Items = comboDtos,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
                    SearchTerm = request.SearchTerm
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving paginated combos: {ex.Message}", ex);
            }
        }
      
        public async Task<DeviceTemplateComboDto> GetComboByIdAsync(long comboId)
        {
            try
            {
                var combo = await _context.DeviceTemplateCombos
                    .Include(c => c.Device)
                    .Include(c => c.Template)
                    .Where(c => c.Id == comboId)
                    .Select(c => new DeviceTemplateComboDto
                    {
                        Id = c.Id,
                        DeviceId = c.DeviceId,
                        TemplateId = c.TemplateId,
                        DeviceName = c.Device.Name,
                        DeviceMac = c.Device.MACAddress,
                        TemplateName = c.Template.Name,
                        ScreenWidth = c.Template.ScreenWidth,
                        ScreenHeight = c.Template.ScreenHeight,
                        ScreenInch = c.Template.ScreenInch,
                        Battery = c.Device.Battery,
                        IsDefault = c.IsDefault,
                        Priority = c.Priority,
                        CreatedDate = c.CreatedDate,
                        IsActive = c.IsActive
                    })
                    .FirstOrDefaultAsync();

                return combo;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving combo by ID: {ex.Message}", ex);
            }
        }

        public async Task<DeviceTemplateComboDto> GetComboByDeviceIdAsync(long deviceId)
        {
            try
            {
                var combo = await _context.DeviceTemplateCombos
                    .Include(c => c.Device)
                    .ThenInclude(d => d.Store) // Include Store for later use
                    .Include(c => c.Template)
                    .Where(c => c.DeviceId == deviceId && c.IsActive)
                    .OrderByDescending(c => c.IsDefault) // Default combo first
                    .ThenByDescending(c => c.Priority) // Higher priority first
                    .ThenByDescending(c => c.CreatedDate) // Most recent first
                    .Select(c => new DeviceTemplateComboDto
                    {
                        Id = c.Id,
                        DeviceId = c.DeviceId,
                        TemplateId = c.TemplateId,
                        DeviceName = c.Device.Name,
                        DeviceMac = c.Device.MACAddress,
                        TemplateName = c.Template.Name,
                        ScreenWidth = c.Template.ScreenWidth,
                        ScreenHeight = c.Template.ScreenHeight,
                        ScreenInch = c.Template.ScreenInch,
                        Battery = c.Device.Battery,
                        IsDefault = c.IsDefault,
                        Priority = c.Priority,
                        CreatedDate = c.CreatedDate,
                        IsActive = c.IsActive,
                        Device = new DeviceDto
                        {
                            Id = c.Device.Id,
                            Mac = c.Device.MACAddress,
                            DeviceName = c.Device.Name,
                            ScreenInch = c.Device.DeviceScreen.Inch,
                            ScreenHeight = c.Device.DeviceScreen.Height,
                            ScreenWidth = c.Device.DeviceScreen.Width,
                            Status = c.Device.Status != null ? c.Device.Status.Name : "Unknown",
                            Battery = c.Device.Battery,
                            LastSeen = c.Device.LastSeen,
                            StoreId = c.Device.StoreId,
                            StoreName = c.Device.Store != null ? c.Device.Store.StoreName : string.Empty,
                            NetworkName = c.Device.NetworkName,
                            IPAddress = c.Device.IPAddress,
                            Firmware = c.Device.Firmware,
                            Hardware = c.Device.Hardware,
                            DeviceType = c.Device.DeviceType
                        }
                    })
                    .FirstOrDefaultAsync();

                return combo;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving combo by device ID: {ex.Message}", ex);
            }
        }

        public async Task<DeviceTemplateCombos> UpdateDeviceTemplateCombosAsync(long id, UpdateDeviceTemplateComboDto dto)
        {
            try
            {
                var existing = await _context.DeviceTemplateCombos
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (existing == null)
                {
                    throw new KeyNotFoundException($"DeviceTemplateCombo with ID {id} not found");
                }

                // Check if new combination already exists (excluding current record)
                var exists = await _context.DeviceTemplateCombos
                    .AnyAsync(d => d.DeviceId == dto.DeviceId &&
                                  d.TemplateId == dto.TemplateId &&
                                  d.Id != id &&
                                  d.IsActive);

                if (exists)
                {
                    throw new InvalidOperationException(
                        $"Another DeviceTemplateCombo with DeviceId: {dto.DeviceId} and TemplateId: {dto.TemplateId} already exists");
                }

                // Update properties
                existing.DeviceId = dto.DeviceId;
                existing.TemplateId = dto.TemplateId;
                existing.IsDefault = dto.IsDefault;
                existing.IsActive = dto.IsActive;
                existing.UpdatedDate = DateTime.UtcNow;
                existing.UpdatedUser = GetCurrentUserId(); // Implement this method

                _context.DeviceTemplateCombos.Update(existing);
                await _context.SaveChangesAsync();

                return existing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating DeviceTemplateCombo with ID: {Id}", id);
                throw;
            }
        }

        // Helper mapping methods
        private DeviceDto MapToDto(DeviceMaster device)
        {
            return new DeviceDto
            {
                Id = device.Id,
                Mac = device.MACAddress ?? string.Empty,
                DeviceName = device.Name ?? string.Empty,
                ScreenSize = device.DeviceScreen.Inch,
                ScreenInch = device.DeviceScreen.Inch,
                ScreenHeight = device.DeviceScreen.Height,
                ScreenWidth = device.DeviceScreen.Width,
                ScreenColor = device.ScreenColor ?? string.Empty,
                Status = device.Status?.Name ?? "Unknown",
                Battery = device.Battery,
                LastSeen = device.LastSeen,
                StoreId = device.StoreId,
                IsOnline = device.StatusId == 0,
                StoreName = device.Store?.StoreName ?? string.Empty,
                NetworkName = device.NetworkName ?? string.Empty,
                IPAddress = device.IPAddress ?? string.Empty,
                Firmware = device.Firmware ?? string.Empty,
                Hardware = device.Hardware ?? string.Empty,
                DeviceType = device.DeviceType ?? string.Empty,
                CreatedDate = device.CreatedDate,
                CreatedUser = device.CreatedUser.ToString()
            };
        }

        private TemplateDto MapTemplateToDto(MinewTemplates template)
        {
            return new TemplateDto
            {
                Id = template.Id,
                Name = template.Name ?? string.Empty,
                Description = template.Description ?? string.Empty,
                ScreenInch = template.ScreenInch,
                ScreenWidth = template.ScreenWidth,
                ScreenHeight = template.ScreenHeight,
                Color = template.Color ?? string.Empty,
                Orientation = template.Orientation,
                PreviewImage = template.PreviewImage ?? string.Empty,
                StoreId = template.StoreId,
                IsActive = template.IsActive,
                SyncDate = template.SyncDate,
                CreatedDate = template.CreatedDate
            };
        }

        public async Task<DeviceDto> GetDeviceByIdAsync(long deviceId)
        {
            try
            {
                var device = await _context.DeviceMaster
                             .Include(d => d.Store)
                             .Include(d => d.Status)
                             .Include(d => d.DeviceScreen)
                             .Where(d => d.IsActive)
                             .FirstOrDefaultAsync(x => x.Id == deviceId);
                if (device == null)
                    return null;

                // Map DeviceMaster entity to DeviceDto
                return new DeviceDto
                {
                    Id = device.Id,
                    Mac = device.MACAddress,
                    DeviceName = device.Name,
                    ScreenSize = null, // Not in entity, or calculate from width/height if needed
                    ScreenInch = device.DeviceScreen.Inch,
                    ScreenHeight = device.DeviceScreen.Height,
                    ScreenWidth = device.DeviceScreen.Width,
                    ScreenColor = device.ScreenColor,
                    Status = device.Status?.Name ?? string.Empty, // Assuming Status has a Name property
                    Battery = device.Battery,
                    LastSeen = device.LastSeen,
                    StoreId = device.StoreId,
                    IsOnline = device.IsOnline,
                    StoreName = device.Store?.StoreName ?? string.Empty, // Assuming Store has StoreName
                    NetworkName = device.NetworkName,
                    IPAddress = device.IPAddress,
                    Firmware = device.Firmware,
                    Hardware = device.Hardware,
                    DeviceType = device.DeviceType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving device ID {Id}: {Message}", deviceId, ex.Message);
                throw;
            }
        }

        public async Task<(bool success, string message)> DeleteComboAsync(long id, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Check if combo has active assignment
                var hasAssignment = await _context.DeviceAssignment
                    .AnyAsync(x => x.DeviceTemplateComboId == id && x.IsActive);

                if (hasAssignment)
                {
                    return (false, "Cannot delete combo: it is used in active assignment.");
                }

              
                // Fetch combo
                var device = await _context.DeviceTemplateCombos.FirstOrDefaultAsync(d => d.Id == id);
                if (device == null)
                    throw new KeyNotFoundException($"Combo with ID {id} not found");

                // Soft delete
                device.IsActive = false;
                device.UpdatedDate = DateTime.UtcNow;
                device.UpdatedUser = userId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Combo deleted successfully");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting combo ID {Id}: {Message}", id, ex.Message);
                throw;
            }
        }

        // Mirrors DeviceController.CreateCombo's reuse-or-create logic, factored
        // into the repository so it can be called from a transactional orchestrator
        // (e.g. ProductRepository.SaveProductWithEslAsync) sharing the same DbContext
        // and ambient transaction, rather than only from its own HTTP endpoint.
        public async Task<(DeviceTemplateCombos Combo, bool AlreadyExisted)> CreateOrReuseTemplateComboAsync(long deviceId, string templateId, bool isDefault)
        {
            var device = await _context.DeviceMaster.FirstOrDefaultAsync(d => d.Id == deviceId);
            if (device == null)
                throw new NotFoundException($"Device with ID {deviceId} not found");

            var template = await _context.MinewTemplates.FirstOrDefaultAsync(t => t.Id == templateId);
            if (template == null)
                throw new NotFoundException($"Template with ID {templateId} not found");

            var existing = await _context.DeviceTemplateCombos
                .FirstOrDefaultAsync(c => c.DeviceId == deviceId && c.TemplateId == templateId);

            if (existing != null) return (existing, true);

            var combo = new DeviceTemplateCombos
            {
                DeviceId = deviceId,
                TemplateId = templateId,
                IsDefault = isDefault,
                Priority = isDefault ? 1 : 0,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _context.DeviceTemplateCombos.Add(combo);
            await _context.SaveChangesAsync();
            return (combo, false);
        }

        public async Task<TemplateDto> GetTempalteByIdAsync(string templateId)
        {
            var template = await _context.MinewTemplates
                               .Include(d => d.Store)
                               .Where(t => t.IsActive && t.Id == templateId.Trim())
                               .FirstOrDefaultAsync();
            if (template == null)
                return null;

            // Map MinewTemplates entity to TemplateDto
            return new TemplateDto
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description ?? string.Empty,
                ScreenInch = template.ScreenInch,
                ScreenWidth = template.ScreenWidth,
                ScreenHeight = template.ScreenHeight,
                Color = template.Color ?? string.Empty,
                Orientation = template.Orientation,
                PreviewImage = template.PreviewImage ?? string.Empty,
                StoreId = template.StoreId,
                IsActive = template.IsActive,
                SyncDate = template.SyncDate,
                CreatedDate = template.CreatedDate
            };
        }

        public async Task<DeviceMessageCombos> GetByIdAsync(long id)
        {
            try
            {
                return await _context.DeviceMessageCombos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting DeviceMessageCombo by ID: {Id}", id);
                throw;
            }
        }

        #region Device - Message Combination Methods
        public async Task<List<DeviceMessageCombos>> GetAllAsync()
        {
            try
            {
                return await _context.DeviceMessageCombos
                    .AsNoTracking()
                    .Where(d => d.IsActive)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all DeviceMessageCombos");
                throw;
            }
        }

        //public async Task<PagedResult<DeviceMessageComboDto>> GetPagedAsync(DeviceMessageComboPagedRequest request)
        //{
        //    try
        //    {
        //        var query = _context.DeviceMessageCombos
        //                  .Include(dmc => dmc.Device)
        //                  .Include(dmc => dmc.Message)
        //                      .ThenInclude(m => m.ContentTypes) 
        //                  .Include(dmc => dmc.Store) 
        //                  .AsQueryable();

        //        // Apply filters
        //        if (request.StoreId.HasValue)
        //        {
        //            query = query.Where(d => d.StoreId == request.StoreId.Value);
        //        }
        //        if (request.DeviceId.HasValue)
        //        {
        //            query = query.Where(d => d.DeviceId == request.DeviceId.Value);
        //        }

        //        if (request.MessageId.HasValue)
        //        {
        //            query = query.Where(d => d.MessageId == request.MessageId.Value);
        //        }

        //        if (request.IsActive.HasValue)
        //        {
        //            query = query.Where(d => d.IsActive == request.IsActive.Value);
        //        }

        //        if (request.CreatedFrom.HasValue)
        //        {
        //            query = query.Where(d => d.CreatedDate >= request.CreatedFrom.Value);
        //        }

        //        if (request.CreatedTo.HasValue)
        //        {
        //            query = query.Where(d => d.CreatedDate <= request.CreatedTo.Value);
        //        }

        //        // Apply search term if provided
        //        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        //        {
        //            query = query.Where(d =>
        //                d.Id.ToString().Contains(request.SearchTerm) ||
        //                d.DeviceId.ToString().Contains(request.SearchTerm) ||
        //                d.MessageId.ToString().Contains(request.SearchTerm));
        //        }

        //        // Apply sorting
        //        if (!string.IsNullOrWhiteSpace(request.SortBy))
        //        {
        //            query = request.SortBy.ToLower() switch
        //            {
        //                "deviceid" => request.SortDescending
        //                    ? query.OrderByDescending(d => d.DeviceId)
        //                    : query.OrderBy(d => d.DeviceId),
        //                "messageid" => request.SortDescending
        //                    ? query.OrderByDescending(d => d.MessageId)
        //                    : query.OrderBy(d => d.MessageId),
        //                "createdat" => request.SortDescending
        //                    ? query.OrderByDescending(d => d.CreatedDate)
        //                    : query.OrderBy(d => d.CreatedDate),
        //                _ => request.SortDescending
        //                    ? query.OrderByDescending(d => d.Id)
        //                    : query.OrderBy(d => d.Id)
        //            };
        //        }
        //        else
        //        {
        //            query = query.OrderByDescending(d => d.Id);
        //        }

        //        // Get total count
        //        var totalCount = await query.CountAsync();

        //        // FOR SQL SERVER 2008: Fetch ALL matching records
        //        var allCombos = await query.ToListAsync();

        //        //// Apply pagination in memory
        //        //var pagedCombos = allCombos
        //        //    .Skip((request.PageNumber - 1) * request.PageSize)
        //        //    .Take(request.PageSize)
        //        //    .ToList();

        //        // Apply pagination in memory
        //        var pagedCombos = allCombos
        //            .Skip((request.PageNumber - 1) * request.PageSize)
        //            .Take(request.PageSize)
        //            .Select(dmc => new DeviceMessageComboDto
        //            {
        //                Id = dmc.Id,
        //                Type = "MESSAGE",
        //                DeviceId = dmc.DeviceId,
        //                DeviceName = dmc.Device != null ? dmc.Device.Name : "Unknown Device",
        //                DeviceHeight = dmc.Device?.DeviceScreen.Height,
        //                DeviceWidth = dmc.Device?.DeviceScreen.Width,
        //                DeviceMac = dmc.Device != null ? dmc.Device.MACAddress : "No MAC",
        //                //DeviceIPAddress = dmc.Device?.IPAddress,
        //                MessageId = dmc.MessageId,
        //                MessageTitle = dmc.Message != null ? dmc.Message.Title : "Unknown Message",
        //                MessageContent = dmc.Message?.ContentData,
        //                //MessageType = dmc.Message?.ContentTypes?.Name,
        //                //ScreenWidth = dmc.Device?.DeviceScreen?.Width,
        //                //ScreenHeight = dmc.Device?.DeviceScreen?.Height,
        //                //ScreenInch = dmc.Device?.DeviceScreen?.Inch,
        //                Battery = dmc.Device?.Battery,
        //                IsDefault = false, // For message combos, set to false or adjust based on your logic
        //                Priority = 0, // Set appropriate priority
        //                CreatedDate = dmc.CreatedDate,
        //                IsActive = dmc.IsActive,
        //                //Device = dmc.Device != null ? new DeviceDto
        //                //{
        //                //    Id = dmc.Device.Id,
        //                //    DeviceName = dmc.Device.Name,
        //                //    Mac = dmc.Device.MACAddress,
        //                //    IPAddress = dmc.Device.IPAddress,
        //                //    Battery = dmc.Device.Battery,
        //                //    Status = dmc.Device.Status.Name,
        //                //    // Add other device properties as needed
        //                //} : null,
        //                StoreName = dmc.Store != null ? dmc.Store.StoreName : null,
        //                StoreId = dmc.StoreId
        //            })
        //            .ToList();

        //        return new PagedResult<DeviceMessageComboDto>
        //        {
        //            Items = pagedCombos,
        //            TotalCount = totalCount,
        //            PageNumber = request.PageNumber,
        //            PageSize = request.PageSize,
        //            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
        //            SearchTerm = request.SearchTerm
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting paginated DeviceMessageCombos");
        //        throw;
        //    }
        //}

        public async Task<PagedResult<DeviceMessageComboDto>> GetPagedAsync(DeviceMessageComboPagedRequest request)
        {
            try
            {
                var query = _context.DeviceMessageCombos
                    .Include(dmc => dmc.Device)
                        .ThenInclude(d => d.DeviceScreen) // Add this include
                    .Include(dmc => dmc.Message)
                        .ThenInclude(m => m.ContentTypes)
                    .Include(dmc => dmc.Store)
                    .AsQueryable();

                // Apply filters
                if (request.StoreId.HasValue)
                {
                    query = query.Where(d => d.StoreId == request.StoreId.Value);
                }
                if (request.DeviceId.HasValue)
                {
                    query = query.Where(d => d.DeviceId == request.DeviceId.Value);
                }

                if (request.MessageId.HasValue)
                {
                    query = query.Where(d => d.MessageId == request.MessageId.Value);
                }

                if (request.IsActive.HasValue)
                {
                    query = query.Where(d => d.IsActive == request.IsActive.Value);
                }

                if (request.CreatedFrom.HasValue)
                {
                    query = query.Where(d => d.CreatedDate >= request.CreatedFrom.Value);
                }

                if (request.CreatedTo.HasValue)
                {
                    query = query.Where(d => d.CreatedDate <= request.CreatedTo.Value);
                }

                // Apply search term if provided
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    query = query.Where(d =>
                        d.Id.ToString().Contains(request.SearchTerm) ||
                        d.DeviceId.ToString().Contains(request.SearchTerm) ||
                        d.MessageId.ToString().Contains(request.SearchTerm) || d.Device.Name.Trim().Contains(request.SearchTerm) || d.Message.Title.Trim().Contains(request.SearchTerm));
                }

                // Apply sorting
                if (!string.IsNullOrWhiteSpace(request.SortBy))
                {
                    query = request.SortBy.ToLower() switch
                    {
                        "deviceid" => request.SortDescending
                            ? query.OrderByDescending(d => d.DeviceId)
                            : query.OrderBy(d => d.DeviceId),
                        "messageid" => request.SortDescending
                            ? query.OrderByDescending(d => d.MessageId)
                            : query.OrderBy(d => d.MessageId),
                        "createdat" => request.SortDescending
                            ? query.OrderByDescending(d => d.CreatedDate)
                            : query.OrderBy(d => d.CreatedDate),
                        _ => request.SortDescending
                            ? query.OrderByDescending(d => d.Id)
                            : query.OrderBy(d => d.Id)
                    };
                }
                else
                {
                    query = query.OrderByDescending(d => d.Id);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Fetch ALL matching records (for SQL Server 2008)
                var allCombos = await query.ToListAsync();

                // Apply pagination and projection in memory
                var pagedCombos = allCombos
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(dmc => new DeviceMessageComboDto
                    {
                        Id = dmc.Id,
                        Type = "MESSAGE",
                        DeviceId = dmc.DeviceId,
                        DeviceName = dmc.Device?.Name ?? "Unknown Device", // Using null-coalescing
                        DeviceHeight = dmc.Device?.DeviceScreen?.Height, // Added ? after DeviceScreen
                        DeviceWidth = dmc.Device?.DeviceScreen?.Width, // Added ? after DeviceScreen
                        DeviceMac = dmc.Device?.MACAddress ?? "No MAC",
                        MessageId = dmc.MessageId,
                        MessageType = dmc.Message?.ContentTypes?.Name ?? "Unknown Type",
                        MessageTitle = dmc.Message?.Title ?? "Unknown Message",
                        MessageContent = dmc.Message?.ContentData, // Already safe
                        Battery = dmc.Device?.Battery,
                        IsDefault = false,
                        Priority = 0,
                        CreatedDate = dmc.CreatedDate,
                        IsActive = dmc.IsActive,
                        StoreName = dmc.Store?.StoreName, // Can be null
                        StoreId = dmc.StoreId
                    })
                    .ToList();

                return new PagedResult<DeviceMessageComboDto>
                {
                    Items = pagedCombos,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
                    SearchTerm = request.SearchTerm
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated DeviceMessageCombos");
                throw;
            }
        }

        //public async Task<DeviceMessageCombos> CreateAsync(DeviceMessageCombos entity)
        //{
        //    try
        //    {
        //        // Check if combination already exists
        //        var exists = await _context.DeviceMessageCombos
        //            .AnyAsync(d => d.DeviceId == entity.DeviceId &&
        //                           d.MessageId == entity.MessageId &&
        //                           d.IsActive);

        //        if (exists)
        //        {
        //            throw new InvalidOperationException(
        //                $"DeviceMessageCombo with DeviceId: {entity.DeviceId} and MessageId: {entity.MessageId} already exists");
        //        }

        //        entity.CreatedDate = DateTime.UtcNow;
        //        entity.UpdatedDate = DateTime.UtcNow;

        //        await _context.DeviceMessageCombos.AddAsync(entity);
        //        await _context.SaveChangesAsync();

        //        return entity;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error creating DeviceMessageCombo");
        //        throw;
        //    }
        //}

        public async Task<DeviceMessageCombos> CreateAsync(CreateDeviceMessageComboDto dto)
        {
            try
            {
                // Check if combination already exists - return it instead of throwing,
                // mirroring DeviceController.CreateCombo's reuse behavior for template combos.
                var existing = await _context.DeviceMessageCombos
                    .FirstOrDefaultAsync(d => d.DeviceId == dto.DeviceId &&
                                   d.MessageId == dto.MessageId &&
                                   d.IsActive);

                if (existing != null)
                {
                    return existing;
                }

                var entity = new DeviceMessageCombos
                {
                    DeviceId = dto.DeviceId,
                    MessageId = dto.MessageId,
                    StoreId = dto.StoreId,
                    IsActive = dto.IsActive,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                await _context.DeviceMessageCombos.AddAsync(entity);
                await _context.SaveChangesAsync();

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating DeviceMessageCombo");
                throw;
            }
        }

        public async Task<DeviceMessageCombos> UpdateAsync(long id, UpdateDeviceMessageComboDto dto)
        {
            try
            {
                var existing = await _context.DeviceMessageCombos
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (existing == null)
                {
                    throw new KeyNotFoundException($"DeviceMessageCombo with ID {id} not found");
                }

                // Check if new combination already exists (excluding current record)
                var exists = await _context.DeviceMessageCombos
                    .AnyAsync(d => d.DeviceId == dto.DeviceId &&
                                   d.MessageId == dto.MessageId &&
                                   d.Id != id &&
                                   d.IsActive);

                if (exists)
                {
                    throw new InvalidOperationException(
                        $"Another DeviceMessageCombo with DeviceId: {dto.DeviceId} and MessageId: {dto.MessageId} already exists");
                }

                existing.DeviceId = dto.DeviceId;
                existing.MessageId = dto.MessageId;
                existing.IsActive = dto.IsActive;
                existing.UpdatedDate = DateTime.UtcNow;
                existing.UpdatedUser = GetCurrentUserId(); // Add this

                _context.DeviceMessageCombos.Update(existing);
                await _context.SaveChangesAsync();

                return existing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating DeviceMessageCombo with ID: {Id}", id);
                throw;
            }
        }

        public async Task<(bool success, string message)> DeleteMessageComboAsync(long id, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Check if message combo has active assignment
                var hasAssignment = await _context.DeviceAssignment
                    .AnyAsync(x => x.DeviceMessageComboId == id && x.IsActive);

                if (hasAssignment)
                {
                    return (false, "Cannot delete message combo: it is used in active assignment.");
                }


                // Fetch combo
                var device = await _context.DeviceMessageCombos.FirstOrDefaultAsync(d => d.Id == id);
                if (device == null)
                    throw new KeyNotFoundException($"Message Combo with ID {id} not found");

                // Soft delete
                device.IsActive = false;
                device.UpdatedDate = DateTime.UtcNow;
                device.UpdatedUser = userId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Combo deleted successfully");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting combo ID {Id}: {Message}", id, ex.Message);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(long id)
        {
            try
            {
                var entity = await _context.DeviceMessageCombos
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (entity == null)
                {
                    return false;
                }

                _context.DeviceMessageCombos.Remove(entity);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting DeviceMessageCombo with ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(long deviceId, long messageId)
        {
            try
            {
                return await _context.DeviceMessageCombos
                    .AnyAsync(d => d.DeviceId == deviceId &&
                                   d.MessageId == messageId &&
                                   d.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if DeviceMessageCombo exists");
                throw;
            }
        }

        public async Task<List<DeviceMessageCombos>> GetByDeviceIdAsync(long deviceId)
        {
            try
            {
                return await _context.DeviceMessageCombos
                    .Where(d => d.DeviceId == deviceId && d.IsActive)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting DeviceMessageCombos by DeviceId: {DeviceId}", deviceId);
                throw;
            }
        }

        public async Task<List<DeviceMessageCombos>> GetByMessageIdAsync(long messageId)
        {
            try
            {
                return await _context.DeviceMessageCombos
                    .Where(d => d.MessageId == messageId && d.IsActive)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting DeviceMessageCombos by MessageId: {MessageId}", messageId);
                throw;
            }
        }

        public async Task<bool> DeactivateByDeviceIdAsync(long deviceId)
        {
            try
            {
                var combos = await _context.DeviceMessageCombos
                    .Where(d => d.DeviceId == deviceId && d.IsActive)
                    .ToListAsync();

                if (!combos.Any())
                {
                    return false;
                }

                foreach (var combo in combos)
                {
                    combo.IsActive = false;
                    combo.UpdatedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating DeviceMessageCombos by DeviceId: {DeviceId}", deviceId);
                throw;
            }
        }

        public async Task<int> GetTotalCountAsync()
        {
            try
            {
                return await _context.DeviceMessageCombos.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total count of DeviceMessageCombos");
                throw;
            }
        }

        public async Task<List<DeviceMessageCombos>> CreateBulkAsync(List<CreateDeviceMessageComboDto> dtos)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var entities = new List<DeviceMessageCombos>();
                var now = DateTime.UtcNow;

                foreach (var dto in dtos)
                {
                    // Check if combination already exists
                    var exists = await _context.DeviceMessageCombos
                        .AnyAsync(d => d.DeviceId == dto.DeviceId &&
                                       d.MessageId == dto.MessageId &&
                                       d.IsActive);

                    if (exists)
                    {
                        _logger.LogWarning(
                            "DeviceMessageCombo with DeviceId: {DeviceId} and MessageId: {MessageId} already exists, skipping",
                            dto.DeviceId, dto.MessageId);
                        continue;
                    }

                    entities.Add(new DeviceMessageCombos
                    {
                        DeviceId = dto.DeviceId,
                        MessageId = dto.MessageId,
                        IsActive = dto.IsActive,
                        CreatedDate = now,
                        UpdatedDate = now,
                        CreatedUser = 0,
                        UpdatedUser = 0
                    });
                }

                if (entities.Any())
                {
                    await _context.DeviceMessageCombos.AddRangeAsync(entities);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return entities;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating bulk DeviceMessageCombos");
                throw;
            }
        }

        public async Task<bool> DeactivateMultipleAsync(List<long> ids)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var combos = await _context.DeviceMessageCombos
                    .Where(d => ids.Contains(d.Id) && d.IsActive)
                    .ToListAsync();

                if (!combos.Any())
                {
                    return false;
                }

                var now = DateTime.UtcNow;
                foreach (var combo in combos)
                {
                    combo.IsActive = false;
                    combo.UpdatedDate = now;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deactivating multiple DeviceMessageCombos");
                throw;
            }
        }

        private int GetCurrentUserId()
        {
            // This depends on how you store user info in your claims
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return 0; // Default or throw exception based on your requirements
        }

        #endregion

        #region Device Assignment Methods
        public async Task<PagedResult<AssignmentViewModel>> GetAssignmentsPagedAsync(AssignmentPagedRequest request)
        {
            try
            {
                var assignments = new List<AssignmentViewModel>();
                int totalCount = 0;

                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("sp_GetAssignments", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters (make sure your SP has the same)
                        command.Parameters.Add(new SqlParameter("@PageNumber", request.PageNumber));
                        command.Parameters.Add(new SqlParameter("@PageSize", request.PageSize));
                        command.Parameters.Add(new SqlParameter("@AssignmentType",
                            string.IsNullOrWhiteSpace(request.AssignmentType) ? DBNull.Value : request.AssignmentType));
                        command.Parameters.Add(new SqlParameter("@LocationType",
                            string.IsNullOrWhiteSpace(request.LocationType) ? DBNull.Value : request.LocationType));
                        command.Parameters.Add(new SqlParameter("@LocationId",
                            request.LocationId.HasValue ? request.LocationId.Value : DBNull.Value));
                        command.Parameters.Add(new SqlParameter("@StoreId",
                            request.StoreId.HasValue ? request.StoreId.Value : DBNull.Value));
                        command.Parameters.Add(new SqlParameter("@DeviceTemplateComboId",
                            request.DeviceTemplateComboId.HasValue ? request.DeviceTemplateComboId.Value : DBNull.Value));
                        command.Parameters.Add(new SqlParameter("@DeviceMessageComboId",
                            request.DeviceMessageComboId.HasValue ? request.DeviceMessageComboId.Value : DBNull.Value));
                        command.Parameters.Add(new SqlParameter("@IsActive",
                            request.IsActive.HasValue ? request.IsActive.Value : DBNull.Value));
                        command.Parameters.Add(new SqlParameter("@SearchTerm",
                            string.IsNullOrWhiteSpace(request.SearchTerm) ? DBNull.Value : request.SearchTerm));

                        // Output parameter
                        command.Parameters.Add(new SqlParameter("@TotalCount", SqlDbType.Int) { Direction = ParameterDirection.Output });

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var vm = new AssignmentViewModel
                                {
                                    Id = reader.GetInt64("Id"),
                                    AssignmentType = reader.GetString("AssignmentType"),
                                    DeviceTemplateComboId = reader.IsDBNull("DeviceTemplateComboId") ? null : reader.GetInt64("DeviceTemplateComboId"),
                                    DeviceMessageComboId = reader.IsDBNull("DeviceMessageComboId") ? null : reader.GetInt64("DeviceMessageComboId"),

                                    DeviceId = reader.GetInt64("DeviceId"),
                                    DeviceName = reader.GetString("DeviceName"),
                                    DeviceMac = reader.GetString("DeviceMac"),
                                    DeviceHeight = reader.IsDBNull("DeviceHeight") ? null : reader.GetInt32("DeviceHeight"),
                                    DeviceWidth = reader.IsDBNull("DeviceWidth") ? null : reader.GetInt32("DeviceWidth"),
                                    Battery = reader.IsDBNull("Battery") ? null : reader.GetInt32("Battery"),

                                    TemplateId = reader.IsDBNull("TemplateId") ? null : reader.GetString("TemplateId"),
                                    TemplateName = reader.IsDBNull("TemplateName") ? null : reader.GetString("TemplateName"),

                                    MessageId = reader.IsDBNull("MessageId") ? null : reader.GetInt64("MessageId"),
                                    MessageTitle = reader.IsDBNull("MessageTitle") ? null : reader.GetString("MessageTitle"),
                                    MessageContent = reader.IsDBNull("MessageContent") ? null : reader.GetString("MessageContent"),

                                    LocationType = reader.GetString("LocationType"),
                                    LocationId = reader.GetInt64("LocationId"),
                                    LocationName = null,

                                    StoreId = reader.GetInt64("StoreId"),
                                    StoreName = reader.GetString("StoreName"),

                                    DisplayOrder = reader.GetInt32("DisplayOrder"),
                                    IsActive = reader.GetBoolean("IsActive"),
                                    CreatedDate = reader.GetDateTime("CreatedDate"),
                                    CreatedUserId = reader.GetInt32("CreatedUserId"),
                                    CreatedUser = reader.GetString("CreatedUser"),
                                    UpdatedDate = reader.IsDBNull("UpdatedDate") ? null : reader.GetDateTime("UpdatedDate"),
                                    UpdatedUserId = reader.IsDBNull("UpdatedUserId") ? null : reader.GetInt32("UpdatedUserId"),
                                    UpdatedUser = reader.IsDBNull("UpdatedUser") ? null : reader.GetString("UpdatedUser")
                                };

                                assignments.Add(vm);
                            }
                        }

                        // Get total count
                        if (command.Parameters["@TotalCount"].Value != DBNull.Value)
                            totalCount = Convert.ToInt32(command.Parameters["@TotalCount"].Value);
                    }
                }

                return new PagedResult<AssignmentViewModel>
                {
                    Items = assignments,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
                    SearchTerm = request.SearchTerm
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving paged assignments: {ex.Message}", ex);
            }
        }




        //public async Task<PagedResult<AssignmentDto>> GetAssignmentsPagedAsync(AssignmentPagedRequest request)
        //{
        //    try
        //    {
        //        // Separate queries for Template and Message assignments
        //        IQueryable<AssignmentViewModel> templateQuery = null;
        //        IQueryable<AssignmentViewModel> messageQuery = null;

        //        // Build Template assignments query (only if needed)
        //        if (string.IsNullOrEmpty(request.AssignmentType) || request.AssignmentType.ToUpper() == "TEMPLATE")
        //        {
        //            templateQuery = _context.DeviceAssignment
        //                .Where(a => a.AssignmentType == "TEMPLATE" && a.DeviceTemplateComboId.HasValue)
        //                .Join(_context.DeviceTemplateCombos,
        //                    assignment => assignment.DeviceTemplateComboId.Value,
        //                    combo => combo.Id,
        //                    (assignment, combo) => new { assignment, combo })
        //                .Join(_context.DeviceMaster,
        //                    x => x.combo.DeviceId,
        //                    device => device.Id,
        //                    (x, device) => new { x.assignment, x.combo, device })
        //                .Join(_context.MinewTemplates,
        //                    x => x.combo.TemplateId,
        //                    template => template.Id,
        //                    (x, template) => new { x.assignment, x.combo, x.device, template })
        //                .GroupJoin(_context.Users,
        //                    x => x.assignment.CreatedUser,
        //                    user => user.Id,
        //                    (x, users) => new { x.assignment, x.combo, x.device, x.template, users })
        //                .SelectMany(
        //                    x => x.users.DefaultIfEmpty(),
        //                    (x, user) => new AssignmentViewModel
        //                    {
        //                        Id = x.assignment.Id,
        //                        AssignmentType = "TEMPLATE",
        //                        DeviceTemplateComboId = x.assignment.DeviceTemplateComboId,
        //                        DeviceMessageComboId = (long?)null
        //                    })
        //                    //    DeviceId = x.device.Id,
        //                    //    DeviceName = x.device.Name,
        //                    //    DeviceHeight = x.device.ScreenHeight,
        //                    //    DeviceWidth = x.device.ScreenWidth,
        //                    //    DeviceMac = x.device.MACAddress,
        //                    //    Battery = x.device.Battery,
        //                    //    TemplateId = x.template.Id,
        //                    //    TemplateName = x.template.Name,
        //                    //    MessageId = (long?)null,
        //                    //    MessageTitle = (string)null,
        //                    //    MessageContent = (string)null,
        //                    //    LocationType = x.assignment.LocationType,
        //                    //    LocationId = x.assignment.LocationId,
        //                    //    DisplayOrder = x.assignment.DisplayOrder,
        //                    //    IsActive = x.assignment.IsActive,
        //                    //    CreatedDate = x.assignment.CreatedDate,
        //                    //    //CreatedUser = user != null ? user.UserName : x.assignment.CreatedUser.ToString(),
        //                    //    CreatedUserId = x.assignment.CreatedUser,
        //                    //    UpdatedDate = x.assignment.UpdatedDate,
        //                    //    UpdatedUserId = x.assignment.UpdatedUser
        //                    //})
        //                .AsQueryable();
        //        }

        //        // Build Message assignments query (only if needed)
        //        if (string.IsNullOrEmpty(request.AssignmentType) || request.AssignmentType.ToUpper() == "MESSAGE")
        //        {
        //            messageQuery = _context.DeviceAssignment
        //                .Where(a => a.AssignmentType == "MESSAGE" && a.DeviceMessageComboId.HasValue)
        //                .Join(_context.DeviceMessageCombos,
        //                    assignment => assignment.DeviceMessageComboId.Value,
        //                    combo => combo.Id,
        //                    (assignment, combo) => new { assignment, combo })
        //                .Join(_context.DeviceMaster,
        //                    x => x.combo.DeviceId,
        //                    device => device.Id,
        //                    (x, device) => new { x.assignment, x.combo, device })
        //                .Join(_context.MessageMaster,
        //                    x => x.combo.MessageId,
        //                    message => message.Id,
        //                    (x, message) => new { x.assignment, x.combo, x.device, message })
        //                .GroupJoin(_context.Users,
        //                    x => x.assignment.CreatedUser,
        //                    user => user.Id,
        //                    (x, users) => new { x.assignment, x.combo, x.device, x.message, users })
        //                .SelectMany(
        //                    x => x.users.DefaultIfEmpty(),
        //                    (x, user) => new AssignmentViewModel
        //                    {
        //                        Id = x.assignment.Id,
        //                        AssignmentType = "MESSAGE",
        //                        DeviceTemplateComboId = (long?)null,
        //                        DeviceMessageComboId = x.assignment.DeviceMessageComboId,
        //                        //DeviceId = x.device.Id,
        //                        //DeviceName = x.device.Name,
        //                        //DeviceHeight = x.device.ScreenHeight,
        //                        //DeviceWidth = x.device.ScreenWidth,
        //                        //DeviceMac = x.device.MACAddress,
        //                        //Battery = x.device.Battery,
        //                        //TemplateId = (string)null,
        //                        //TemplateName = (string)null,
        //                        //MessageId = x.message.Id,
        //                        //MessageTitle = x.message.Title,
        //                        //MessageContent = x.message.ContentData,
        //                        //LocationType = x.assignment.LocationType,
        //                        //LocationId = x.assignment.LocationId,
        //                        //DisplayOrder = x.assignment.DisplayOrder,
        //                        //IsActive = x.assignment.IsActive,
        //                        //CreatedDate = x.assignment.CreatedDate,
        //                        ////CreatedUser = user != null ? user.UserName : x.assignment.CreatedUser.ToString(),
        //                        //CreatedUserId = x.assignment.CreatedUser,
        //                        //UpdatedDate = x.assignment.UpdatedDate,
        //                        //UpdatedUserId = x.assignment.UpdatedUser
        //                    })
        //                .AsQueryable();
        //        }

        //        // Combine queries based on AssignmentType filter
        //        IQueryable<AssignmentViewModel> combinedQuery = null;

        //        if (templateQuery != null && messageQuery != null)
        //        {
        //            combinedQuery = templateQuery.Concat(messageQuery);
        //        }
        //        else if (templateQuery != null)
        //        {
        //            combinedQuery = templateQuery;
        //        }
        //        else if (messageQuery != null)
        //        {
        //            combinedQuery = messageQuery;
        //        }
        //        else
        //        {
        //            // Return empty result if no query was built
        //            return new PagedResult<AssignmentDto>
        //            {
        //                Items = new List<AssignmentDto>(),
        //                TotalCount = 0,
        //                PageNumber = request.PageNumber,
        //                PageSize = request.PageSize,
        //                TotalPages = 0,
        //                SearchTerm = request.SearchTerm
        //            };
        //        }

        //        // Apply filters based on AssignmentType
        //        if (!string.IsNullOrWhiteSpace(request.AssignmentType))
        //        {
        //            combinedQuery = combinedQuery.Where(x => x.AssignmentType == request.AssignmentType.ToUpper());
        //        }

        //        // Apply other filters
        //        if (!string.IsNullOrWhiteSpace(request.LocationType))
        //        {
        //            combinedQuery = combinedQuery.Where(x => x.LocationType.ToLower() == request.LocationType.ToLower());
        //        }

        //        if (request.LocationId.HasValue)
        //        {
        //            combinedQuery = combinedQuery.Where(x => x.LocationId == request.LocationId.Value);
        //        }

        //        if (request.DeviceTemplateComboId.HasValue)
        //        {
        //            combinedQuery = combinedQuery.Where(x => x.DeviceTemplateComboId == request.DeviceTemplateComboId.Value);
        //        }

        //        if (request.DeviceMessageComboId.HasValue)
        //        {
        //            combinedQuery = combinedQuery.Where(x => x.DeviceMessageComboId == request.DeviceMessageComboId.Value);
        //        }

        //        if (request.IsActive.HasValue)
        //        {
        //            combinedQuery = combinedQuery.Where(x => x.IsActive == request.IsActive.Value);
        //        }

        //        // Apply search term
        //        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        //        {
        //            var searchTerm = request.SearchTerm.Trim().ToLower();
        //            combinedQuery = combinedQuery.Where(x =>
        //                (x.DeviceName != null && x.DeviceName.ToLower().Contains(searchTerm)) ||
        //                (x.TemplateName != null && x.TemplateName.ToLower().Contains(searchTerm)) ||
        //                (x.MessageTitle != null && x.MessageTitle.ToLower().Contains(searchTerm)) ||
        //                (x.DeviceMac != null && x.DeviceMac.ToLower().Contains(searchTerm)) ||
        //                (x.CreatedUser != null && x.CreatedUser.ToLower().Contains(searchTerm)) ||
        //                (x.LocationType != null && x.LocationType.ToLower().Contains(searchTerm)) ||
        //                (x.AssignmentType != null && x.AssignmentType.ToLower().Contains(searchTerm))
        //            );
        //        }

        //        // Get total count
        //        var totalCount =  combinedQuery.Count();

        //        // FOR SQL SERVER 2008 COMPATIBILITY: Fetch ALL matching records
        //        var allAssignments = await combinedQuery.ToListAsync();

        //        // Apply sorting in memory
        //        var sortedAssignments = allAssignments.AsQueryable()
        //            .ApplyAssignmentSorting(request.SortBy, request.SortDescending);

        //        // Apply pagination in memory
        //        var pagedAssignments = sortedAssignments
        //            .Skip((request.PageNumber - 1) * request.PageSize)
        //            .Take(request.PageSize)
        //            .ToList();

        //        // Map to DTO
        //        var assignmentDtos = pagedAssignments.Select(x => new AssignmentDto
        //        {
        //            Id = x.Id,
        //            AssignmentType = x.AssignmentType,
        //            DeviceTemplateComboId = x.DeviceTemplateComboId,
        //            DeviceMessageComboId = x.DeviceMessageComboId,
        //            Combo = x.AssignmentType == "TEMPLATE" ?
        //                new DeviceTemplateComboDto
        //                {
        //                    DeviceId = x.DeviceId,
        //                    DeviceName = x.DeviceName,
        //                    DeviceHeight = x.DeviceHeight,
        //                    DeviceWidth = x.DeviceWidth,
        //                    DeviceMac = x.DeviceMac,
        //                    Battery = x.Battery,
        //                    TemplateId = x.TemplateId,
        //                    TemplateName = x.TemplateName
        //                } :
        //                new DeviceMessageComboDto
        //                {
        //                    DeviceId = x.DeviceId,
        //                    DeviceName = x.DeviceName,
        //                    DeviceMac = x.DeviceMac,
        //                    DeviceHeight = x.DeviceHeight,
        //                    DeviceWidth = x.DeviceWidth,
        //                    Battery = x.Battery,
        //                    MessageId = x.MessageId.Value,
        //                    MessageTitle = x.MessageTitle,
        //                    MessageContent = x.MessageContent
        //                },
        //            LocationType = x.LocationType,
        //            LocationId = x.LocationId,
        //            DisplayOrder = x.DisplayOrder,
        //            IsActive = x.IsActive,
        //            CreatedDate = x.CreatedDate,
        //            CreatedUser = x.CreatedUser
        //        }).ToList();

        //        return new PagedResult<AssignmentDto>
        //        {
        //            Items = assignmentDtos,
        //            TotalCount = totalCount,
        //            PageNumber = request.PageNumber,
        //            PageSize = request.PageSize,
        //            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
        //            SearchTerm = request.SearchTerm
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"Error retrieving paginated assignments: {ex.Message}", ex);
        //    }
        //}

        //public async Task<PagedResult<AssignmentDto>> GetAssignmentsPagedAsync(AssignmentPagedRequest request)
        //{
        //    try
        //    {
        //        // Start with base query and map to ViewModel immediately
        //        var query = _context.DeviceAssignment
        //            .Join(_context.DeviceTemplateCombos,
        //                assignment => assignment.DeviceTemplateComboId,
        //                combo => combo.Id,
        //                (assignment, combo) => new { assignment, combo })
        //            .Join(_context.DeviceMaster,
        //                x => x.combo.DeviceId,
        //                device => device.Id,
        //                (x, device) => new { x.assignment, x.combo, device })
        //            .Join(_context.MinewTemplates,
        //                x => x.combo.TemplateId,
        //                template => template.Id,
        //                (x, template) => new { x.assignment, x.combo, x.device, template })
        //            // Add LEFT JOIN to User table
        //            .GroupJoin(_context.Users,
        //                x => x.assignment.CreatedUser,
        //                user => user.Id,
        //                (x, users) => new { x.assignment, x.combo, x.device, x.template, users })
        //            .SelectMany(
        //                x => x.users.DefaultIfEmpty(), // LEFT JOIN
        //                (x, user) => new AssignmentViewModel
        //                {
        //                    Id = x.assignment.Id,
        //                    DeviceTemplateComboId = x.assignment.DeviceTemplateComboId,
        //                    DeviceId = x.combo.DeviceId,
        //                    DeviceName = x.device.Name,
        //                    DeviceHeight = x.device.ScreenHeight,
        //                    DeviceWidth = x.device.ScreenWidth,
        //                    DeviceMac = x.device.MACAddress,
        //                    TemplateId = x.combo.TemplateId,
        //                    TemplateName = x.template.Name,
        //                    LocationType = x.assignment.LocationType,
        //                    LocationId = x.assignment.LocationId,
        //                    DisplayOrder = x.assignment.DisplayOrder,
        //                    IsActive = x.assignment.IsActive,
        //                    CreatedDate = x.assignment.CreatedDate,
        //                    // Get username or fallback to CreatedUser ID if user not found
        //                    CreatedUser = user != null ? user.UserName : x.assignment.CreatedUser.ToString(),
        //                    // Store both ID and name
        //                    CreatedUserId = x.assignment.CreatedUser,
        //                })
        //            .AsQueryable();

        //        // Apply filters
        //        if (!string.IsNullOrWhiteSpace(request.LocationType))
        //        {
        //            query = query.Where(x => x.LocationType.ToLower() == request.LocationType.ToLower());
        //        }

        //        if (request.LocationId.HasValue)
        //        {
        //            query = query.Where(x => x.LocationId == request.LocationId.Value);
        //        }

        //        if (request.DeviceTemplateComboId.HasValue)
        //        {
        //            query = query.Where(x => x.DeviceTemplateComboId == request.DeviceTemplateComboId.Value);
        //        }

        //        if (request.IsActive.HasValue)
        //        {
        //            query = query.Where(x => x.IsActive == request.IsActive.Value);
        //        }

        //        // Apply search term
        //        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        //        {
        //            var searchTerm = request.SearchTerm.Trim().ToLower();
        //            query = query.Where(x =>
        //                (x.DeviceName != null && x.DeviceName.ToLower().Contains(searchTerm)) ||
        //                (x.TemplateName != null && x.TemplateName.ToLower().Contains(searchTerm)) ||
        //                (x.DeviceMac != null && x.DeviceMac.ToLower().Contains(searchTerm)) ||
        //                (x.CreatedUser != null && x.CreatedUser.ToLower().Contains(searchTerm)) ||
        //                (x.LocationType != null && x.LocationType.ToLower().Contains(searchTerm))
        //            );
        //        }

        //        // Get total count
        //        var totalCount = await query.CountAsync();

        //        // FOR SQL SERVER 2008 COMPATIBILITY: Fetch ALL matching records
        //        var allAssignments = await query.ToListAsync();

        //        // Apply sorting in memory
        //        var sortedAssignments = allAssignments.AsQueryable()
        //            .ApplyAssignmentSorting(request.SortBy, request.SortDescending);

        //        // Apply pagination in memory
        //        var pagedAssignments = sortedAssignments
        //            .Skip((request.PageNumber - 1) * request.PageSize)
        //            .Take(request.PageSize)
        //            .ToList();

        //        // Map to DTO
        //        var assignmentDtos = pagedAssignments.Select(x => new AssignmentDto
        //        {
        //            Id = x.Id,
        //            DeviceTemplateComboId = x.DeviceTemplateComboId,
        //            Combo = new DeviceTemplateComboDto
        //            {
        //                DeviceId = x.DeviceId,
        //                DeviceName = x.DeviceName,
        //                DeviceHeight = x.DeviceHeight,
        //                DeviceWidth = x.DeviceWidth,
        //                DeviceMac = x.DeviceMac,
        //                TemplateId = x.TemplateId,
        //                TemplateName = x.TemplateName
        //            },
        //            LocationType = x.LocationType,
        //            LocationId = x.LocationId,
        //            DisplayOrder = x.DisplayOrder,
        //            IsActive = x.IsActive,
        //            CreatedDate = x.CreatedDate,
        //            CreatedUser = x.CreatedUser
        //        }).ToList();

        //        return new PagedResult<AssignmentDto>
        //        {
        //            Items = assignmentDtos,
        //            TotalCount = totalCount,
        //            PageNumber = request.PageNumber,
        //            PageSize = request.PageSize,
        //            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
        //            SearchTerm = request.SearchTerm
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"Error retrieving paginated assignments: {ex.Message}", ex);
        //    }
        //}

        public async Task<AssignmentDto> CreateAssignmentAsync(CreateAssignmentRequest request)
        {
            try
            {
                // Validate request
                ValidateAssignmentRequest(request);

                // Validate combo exists
                await ValidateComboExistsAsync(request);

                // Validate location exists
                bool locationExists = await ValidateLocationExistsAsync(request.LocationType, request.LocationId);
                if (!locationExists)
                    throw new NotFoundException($"Location not found: {request.LocationType}/{request.LocationId}");

                // Check for existing assignment
                var existingAssignment = await GetExistingAssignmentAsync(request);
                if (existingAssignment != null)
                    throw new InvalidOperationException("Combo already assigned to this location");

                // Get next display order
                int nextDisplayOrder = await GetNextDisplayOrderAsync(request.LocationType, request.LocationId);

                // Create assignment
                var assignment = new DeviceAssignment
                {
                    AssignmentType = request.AssignmentType.ToUpper(),
                    LocationType = request.LocationType,
                    LocationId = request.LocationId,
                    DisplayOrder = nextDisplayOrder + 1,
                    StoreId = request.StoreId,
                    IsActive = true,
                    CreatedUser = request.UserId,
                    UpdatedUser = request.UserId,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                // Set the appropriate ID based on assignment type
                if (request.AssignmentType.ToUpper() == "TEMPLATE")
                {
                    assignment.DeviceTemplateComboId = request.DeviceTemplateComboId ?? 0;
                    assignment.DeviceMessageComboId = null; // Or set to a default value
                }
                else // MESSAGE
                {
                    assignment.DeviceTemplateComboId = null; // Or set to a default value
                    assignment.DeviceMessageComboId = request.DeviceMessageComboId ?? 0;
                }


                await _context.DeviceAssignment.AddAsync(assignment);
                await _context.SaveChangesAsync();

                // Get assignment details with location name
                var assignmentDto = await GetAssignmentDetailsAsync(assignment.Id);
                assignmentDto.LocationName = await GetLocationNameAsync(request.LocationType, request.LocationId);

                return assignmentDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating assignment");
                throw;
            }
        }

        public async Task<List<AssignmentDto>> GetAssignmentsAsync(string locationType, long locationId)
        {
            try
            {
                var assignments = await _context.DeviceAssignment
                    .Where(a => a.LocationType.ToLower() == locationType.ToLower() &&
                               a.LocationId == locationId &&
                               a.IsActive)
                    .OrderBy(a => a.DisplayOrder)
                    .ToListAsync();

                var result = new List<AssignmentDto>();

                foreach (var assignment in assignments)
                {
                    var dto = await MapToAssignmentDtoAsync(assignment);
                    dto.LocationName = await GetLocationNameAsync(assignment.LocationType, assignment.LocationId);
                    result.Add(dto);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignments for {LocationType}/{LocationId}", locationType, locationId);
                throw;
            }
        }

        public async Task<AssignmentDto> UpdateAssignmentOrderAsync(long id, AssignmentDto.UpdateAssignmentOrderRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var assignment = await _context.DeviceAssignment
                    .FirstOrDefaultAsync(a => a.Id == id && a.IsActive);

                if (assignment == null)
                    throw new NotFoundException($"Assignment not found with ID: {id}");

                // Get all assignments for the same location
                var assignments = await _context.DeviceAssignment
                    .Where(a => a.LocationType == assignment.LocationType &&
                               a.LocationId == assignment.LocationId &&
                               a.IsActive &&
                               a.Id != id) // Exclude the assignment being moved
                    .OrderBy(a => a.DisplayOrder)
                    .ToListAsync();

                // Create new ordered list
                var orderedAssignments = new List<DeviceAssignment>();

                // Add assignments before new position
                for (int i = 0; i < request.NewPosition && i < assignments.Count; i++)
                {
                    orderedAssignments.Add(assignments[i]);
                }

                // Add the assignment being moved
                orderedAssignments.Add(assignment);

                // Add assignments after new position
                for (int i = request.NewPosition; i < assignments.Count; i++)
                {
                    orderedAssignments.Add(assignments[i]);
                }

                // Update display orders
                for (int i = 0; i < orderedAssignments.Count; i++)
                {
                    orderedAssignments[i].DisplayOrder = i + 1;
                    orderedAssignments[i].UpdatedDate = DateTime.UtcNow;
                    orderedAssignments[i].UpdatedUser = request.UserId;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Return updated assignment
                return await GetAssignmentDetailsAsync(id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating assignment order for ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> RemoveAssignmentAsync(long id, int userId)
        {
            try
            {
                var assignment = await _context.DeviceAssignment
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (assignment == null)
                    throw new NotFoundException($"Assignment not found with ID: {id}");

                assignment.IsActive = false;
                assignment.UpdatedDate = DateTime.UtcNow;
                assignment.UpdatedUser = userId;

                await _context.SaveChangesAsync();

                // Reorder remaining assignments
                await ReorderAssignmentsAsync(assignment.LocationType, assignment.LocationId, userId);

                await UnbindFromMinewAsync(assignment);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing assignment with ID: {Id}", id);
                throw;
            }
        }

        // Tells Minew's cloud to release the tag from whatever data/template it was
        // bound to, so the physical ESL screen stops showing stale data once the
        // local assignment is removed. Failures here are logged but never block
        // the local unassignment - a demo device offline shouldn't break the flow.
        private async Task UnbindFromMinewAsync(DeviceAssignment assignment)
        {
            try
            {
                long? deviceId = assignment.DeviceTemplateComboId.HasValue
                    ? (await _context.DeviceTemplateCombos
                        .FirstOrDefaultAsync(c => c.Id == assignment.DeviceTemplateComboId.Value))?.DeviceId
                    : assignment.DeviceMessageComboId.HasValue
                        ? (await _context.DeviceMessageCombos
                            .FirstOrDefaultAsync(c => c.Id == assignment.DeviceMessageComboId.Value))?.DeviceId
                        : null;

                if (deviceId == null)
                    return;

                var device = await _context.DeviceMaster.FirstOrDefaultAsync(d => d.Id == deviceId.Value);
                if (device == null || string.IsNullOrEmpty(device.MACAddress))
                    return;

                var store = await _context.StoreMaster.FirstOrDefaultAsync(s => s.Id == device.StoreId);
                if (store == null || string.IsNullOrEmpty(store.MinewStoreId))
                {
                    _logger.LogWarning("No MinewStoreId found for device {DeviceId}, skipping Minew unbind", deviceId);
                    return;
                }

                var result = await _minewService.UnbindDeviceAsync(device.MACAddress, store.MinewStoreId);
                if (result?.Code != 200)
                {
                    _logger.LogWarning("Failed to unbind device {Mac} in Minew: {Message}",
                        device.MACAddress, result?.Msg ?? "Unknown error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unbinding device from Minew for assignment {Id}", assignment.Id);
            }
        }

        public async Task<bool> ValidateLocationExistsAsync(string locationType, long locationId)
        {
            try
            {
                return locationType.ToLower() switch
                {
                    "aisle" => await _context.AisleMaster.AnyAsync(a => a.Id == locationId),
                    "shelf" => await _context.ShelfMaster.AnyAsync(s => s.Id == locationId),
                    "product" => await _context.ProductMaster.AnyAsync(p => p.Id == locationId),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating location existence");
                return false;
            }
        }

        public async Task<string> GetLocationNameAsync(string locationType, long locationId)
        {
            try
            {
                return locationType.ToLower() switch
                {
                    "aisle" => await _context.AisleMaster
                        .Where(a => a.Id == locationId)
                        .Select(a => a.Name)
                        .FirstOrDefaultAsync() ?? $"Aisle {locationId}",
                    "shelf" => await _context.ShelfMaster
                        .Where(s => s.Id == locationId)
                        .Select(s => s.Name)
                        .FirstOrDefaultAsync() ?? $"Shelf {locationId}",
                    "product" => await _context.ProductMaster
                        .Where(p => p.Id == locationId)
                        .Select(p => p.ProductName)
                        .FirstOrDefaultAsync() ?? $"Product {locationId}",
                    _ => $"Unknown Location ({locationType}:{locationId})"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location name");
                return $"Error retrieving name for {locationType}:{locationId}";
            }
        }
        #endregion

        #region Gateway handlers
        public async Task<GatewayDto> GetGatewayByIdAsync(long id)
        {
            var gateway = await _context.GatewayMaster
                .Include(g => g.Store)
                .Include(g => g.Status)
                .FirstOrDefaultAsync(g => g.Id == id && g.IsActive);

            if (gateway == null)
                throw new KeyNotFoundException($"Gateway with ID {id} not found");

            return MapToGatewayDto(gateway);
        }

        public async Task<List<GatewayDto>> GetGatewaysByIdsAsync(List<long> ids)
        {
            var gateways = await _context.GatewayMaster
                .Include(g => g.Store)
                .Include(g => g.Status)
                .Where(g => ids.Contains(g.Id) && g.IsActive)
                .ToListAsync();

            return gateways.Select(MapToGatewayDto).ToList();
        }

        public async Task<PagedResult<GatewayDto>> GetGatewaysPagedAsync(GatewayPagedRequest request)
        {
            try
            {
                var query =
                    from g in _context.GatewayMaster
                    where g.IsActive

                    // LEFT JOIN Store
                    join s in _context.StoreMaster
                        on g.StoreId equals s.Id into storeJoin
                    from s in storeJoin.DefaultIfEmpty()

                        // LEFT JOIN Status
                    join st in _context.Status
                        on g.StatusId equals st.Id into statusJoin
                    from st in statusJoin.DefaultIfEmpty()

                        // LEFT JOIN Created User
                    join cu in _context.Users
                        on g.CreatedUser equals cu.Id into cuJoin
                    from cu in cuJoin.DefaultIfEmpty()

                        // LEFT JOIN Updated User
                    join uu in _context.Users
                        on g.UpdatedUser equals uu.Id into uuJoin
                    from uu in uuJoin.DefaultIfEmpty()

                    select new
                    {
                        Gateway = g,
                        StoreName = s != null ? s.StoreName : string.Empty,
                        StatusName = st != null ? st.Name : "Unknown",
                        CreatedUserName = cu != null ? cu.UserName : string.Empty,
                        UpdatedUserName = uu != null ? uu.UserName : string.Empty
                    };

                // ================= FILTERS =================
                if (request.StoreId.HasValue && request.StoreId.Value > 0)
                {
                    query = query.Where(x => x.Gateway.StoreId == request.StoreId.Value);
                }

                if (request.IsOnline.HasValue)
                {
                    query = query.Where(x => x.Gateway.IsOnline == request.IsOnline.Value);
                }

                if (!string.IsNullOrWhiteSpace(request.Status))
                {
                    query = query.Where(x => x.StatusName.Trim() == request.Status.Trim().ToUpper());
                }

                if (request.MinBattery.HasValue)
                {
                    query = query.Where(x => x.Gateway.Battery >= request.MinBattery.Value);
                }

                if (request.MaxBattery.HasValue)
                {
                    query = query.Where(x => x.Gateway.Battery <= request.MaxBattery.Value);
                }

                if (request.LastSeenFrom.HasValue)
                {
                    query = query.Where(x => x.Gateway.LastSeen >= request.LastSeenFrom.Value);
                }

                if (request.LastSeenTo.HasValue)
                {
                    query = query.Where(x => x.Gateway.LastSeen <= request.LastSeenTo.Value);
                }

                if (!string.IsNullOrWhiteSpace(request.GatewayType))
                {
                    query = query.Where(x => x.Gateway.GatewayType == request.GatewayType);
                }

                // ================= SEARCH =================
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var term = request.SearchTerm.Trim().ToLower();

                    query = query.Where(x =>
                        (x.Gateway.Name != null && x.Gateway.Name.ToLower().Contains(term)) ||
                        (x.Gateway.MacAddress != null && x.Gateway.MacAddress.ToLower().Contains(term)) ||
                        (x.Gateway.Description != null && x.Gateway.Description.ToLower().Contains(term)) ||
                        (x.StoreName != null && x.StoreName.ToLower().Contains(term))
                    );
                }

                // ================= COUNT =================
                var totalCount = await query.CountAsync();

                // ================= SQL 2008 SAFE =================
                var rows = await query.ToListAsync();

                // ================= SORT =================
                var sorted = ApplyGatewaySorting(
                    rows.Select(x => x.Gateway).AsQueryable(),
                    request.SortBy,
                    request.SortDescending);

                // ================= PAGE =================
                var paged = sorted
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // ================= MAP =================
                var gatewayDtos = paged
                    .Join(rows,
                        g => g.Id,
                        x => x.Gateway.Id,
                        (g, x) => new GatewayDto
                        {
                            Id = g.Id,
                            MacAddress = g.MacAddress ?? string.Empty,
                            Name = g.Name ?? string.Empty,
                            Description = g.Description,
                            StoreId = g.StoreId,
                            StoreName = x.StoreName,
                            MinewGatewayId = g.MinewGatewayId,
                            StatusId = g.StatusId,
                            Status = x.StatusName,
                            GatewayType = g.GatewayType ?? string.Empty,
                            HardwareVersion = g.HardwareVersion,
                            FirmwareVersion = g.FirmwareVersion,
                            Battery = g.Battery,
                            IsOnline = g.IsOnline,
                            LastSeen = g.LastSeen,
                            LastSyncTime = g.LastSyncTime,
                            IsActive = g.IsActive,
                            CreatedDate = g.CreatedDate,
                            CreatedUser = x.CreatedUserName,
                            UpdatedUser = x.UpdatedUserName
                        })
                    .ToList();

                return new PagedResult<GatewayDto>
                {
                    Items = gatewayDtos,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
                    SearchTerm = request.SearchTerm
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving paginated gateways: {ex.Message}", ex);
            }
        }

        private IQueryable<GatewayMaster> ApplyGatewaySorting(IQueryable<GatewayMaster> query, string? sortBy, bool sortDescending)
        {
            return (sortBy?.ToLower(), sortDescending) switch
            {
                ("name", false) => query.OrderBy(g => g.Name),
                ("name", true) => query.OrderByDescending(g => g.Name),
                ("macaddress", false) => query.OrderBy(g => g.MacAddress),
                ("macaddress", true) => query.OrderByDescending(g => g.MacAddress),
                ("lastseen", false) => query.OrderBy(g => g.LastSeen),
                ("lastseen", true) => query.OrderByDescending(g => g.LastSeen),
                ("createddate", false) => query.OrderBy(g => g.CreatedDate),
                ("createddate", true) => query.OrderByDescending(g => g.CreatedDate),
                ("isonline", false) => query.OrderBy(g => g.IsOnline),
                ("isonline", true) => query.OrderByDescending(g => g.IsOnline),
                _ => query.OrderByDescending(g => g.CreatedDate)
            };
        }

        public async Task<GatewayDto> CreateGatewayAsync(CreateGatewayRequest request)
        {
            // Validate MAC address
            var cleanMac = request.MacAddress.Replace(":", "").Replace("-", "").Replace(" ", "").ToLower();
            if (cleanMac.Length != 12)
                throw new ArgumentException("Invalid MAC address format");

            // Check if gateway already exists
            var existing = await _context.GatewayMaster
                .FirstOrDefaultAsync(g => g.MacAddress == cleanMac && g.IsActive);

            if (existing != null)
                throw new InvalidOperationException($"Gateway with MAC {cleanMac} already exists");

            var gateway = new GatewayMaster
            {
                MacAddress = cleanMac,
                Name = request.Name,
                Description = request.Description,
                StoreId = request.StoreId,
                GatewayType = request.GatewayType,
                HardwareVersion = request.HardwareVersion,
                FirmwareVersion = request.FirmwareVersion,
                Battery = request.Battery,
                StatusId = request.StatusId,
                IsOnline = false, // Default to offline until synced
                IsActive = request.IsActive,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = request.CreatedUser
            };

            _context.GatewayMaster.Add(gateway);
            await _context.SaveChangesAsync();

            return await GetGatewayByIdAsync(gateway.Id);
        }

        public async Task<GatewayDto> UpdateGatewayAsync(long id, UpdateGatewayRequest request)
        {
            var gateway = await _context.GatewayMaster
                .FirstOrDefaultAsync(g => g.Id == id && g.IsActive);

            if (gateway == null)
                throw new KeyNotFoundException($"Gateway with ID {id} not found");

            gateway.Name = request.Name;
            gateway.Description = request.Description;
            gateway.Battery = request.Battery;
            gateway.IsOnline = request.IsOnline;
            gateway.LastSeen = request.LastSeen;
            gateway.IsActive = request.IsActive;
            gateway.UpdatedDate = DateTime.UtcNow;
            gateway.UpdatedUser = request.UpdatedUser;

            await _context.SaveChangesAsync();

            return await GetGatewayByIdAsync(gateway.Id);
        }

        public async Task<(bool success, string message)> DeleteGatewayAsync(long id, int userId)
        {
            var gateway = await _context.GatewayMaster
                .FirstOrDefaultAsync(g => g.Id == id && g.IsActive);

            if (gateway == null)
                return (false, "Gateway not found");

            // Check if gateway is in use (e.g., has connected devices)
            var hasDevices = await _context.DeviceMaster
                .AnyAsync(d => d.StoreId == gateway.StoreId && d.IsActive && d.IsOnline);

            if (hasDevices)
                return (false, "Cannot delete gateway. It has active devices connected.");

            // Soft delete
            gateway.IsActive = false;
            gateway.UpdatedDate = DateTime.UtcNow;
            gateway.UpdatedUser = userId;

            await _context.SaveChangesAsync();
            return (true, "Gateway deleted successfully");
        }

        public async Task SyncGatewaysFromCloudAsync(string token, string minewStoreId, long storeId)
        {
            // Get gateways from Minew Cloud
            //var minewService = _serviceProvider.GetRequiredService<MinewCloudService>();
            var url = $"apis/esl/gateway/listPage?page=1&size=100&storeId={minewStoreId}";

            // Note: You need to implement this method in MinewCloudService
            var response = await _minewService.GetFromMinewAsync<MinewGatewayResponse>(token, url);

            if (response.Code != 200)
                throw new Exception($"Failed to sync gateways from cloud: {response.Msg}");

            foreach (var cloudGateway in response.Items)
            {
                var existing = await _context.GatewayMaster
                    .FirstOrDefaultAsync(g => g.MinewGatewayId == cloudGateway.Id ||
                                             g.MacAddress == cloudGateway.Mac);

                if (existing == null)
                {
                    // Create new gateway
                    var gateway = new GatewayMaster
                    {
                        MacAddress = cloudGateway.Mac,
                        Name = cloudGateway.Name ?? $"GW-{cloudGateway.Mac}",
                        Description = "Synced from Minew Cloud",
                        StoreId = storeId,
                        MinewGatewayId = cloudGateway.Id,
                        GatewayType = "Minew",
                        HardwareVersion = cloudGateway.Hardware,
                        FirmwareVersion = cloudGateway.Firmware,
                        StatusId = cloudGateway.Mode == 1 ? 0 : 1, // 1=online, 0=offline in Minew
                        IsOnline = cloudGateway.Mode == 1,
                        LastSeen = DateTime.TryParse(cloudGateway.UpdateTime, out var parsed) ? parsed : DateTime.UtcNow,
                        LastSyncTime = DateTime.UtcNow,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        CreatedUser = 0 // Set appropriate user ID
                    };
                    _context.GatewayMaster.Add(gateway);
                }
                else
                {
                    // Update existing gateway
                    existing.Name = cloudGateway.Name ?? existing.Name;
                    existing.HardwareVersion = cloudGateway.Hardware ?? existing.HardwareVersion;
                    existing.FirmwareVersion = cloudGateway.Firmware ?? existing.FirmwareVersion;
                    existing.StatusId = cloudGateway.Mode == 1 ? 0 : 1;
                    existing.IsOnline = cloudGateway.Mode == 1;
                    existing.LastSeen = DateTime.TryParse(cloudGateway.UpdateTime, out var parsed) ? parsed : DateTime.UtcNow;
                    existing.LastSyncTime = DateTime.UtcNow;
                    existing.UpdatedDate = DateTime.UtcNow;
                    existing.UpdatedUser = 0; // Set appropriate user ID
                }
            }

            await _context.SaveChangesAsync();
        }

        private GatewayDto MapToGatewayDto(GatewayMaster gateway)
        {
            return new GatewayDto
            {
                Id = gateway.Id,
                MacAddress = gateway.MacAddress,
                Name = gateway.Name,
                Description = gateway.Description,
                StoreId = gateway.StoreId,
                StoreName = gateway.Store?.StoreName,
                MinewGatewayId = gateway.MinewGatewayId,
                StatusId = gateway.StatusId,
                Status = gateway.Status?.Name,
                GatewayType = gateway.GatewayType,
                HardwareVersion = gateway.HardwareVersion,
                FirmwareVersion = gateway.FirmwareVersion,
                Battery = gateway.Battery,
                IsOnline = gateway.IsOnline,
                LastSeen = gateway.LastSeen,
                LastSyncTime = gateway.LastSyncTime,
                IsActive = gateway.IsActive,
                CreatedDate = gateway.CreatedDate,
                CreatedUser = gateway.CreatedUser.ToString(),
                UpdatedUser = gateway.UpdatedUser.ToString()
            };
        }
        #endregion
        #region Private Helper Methods

        private void ValidateAssignmentRequest(CreateAssignmentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AssignmentType))
                throw new ArgumentException("AssignmentType is required");

            var assignmentType = request.AssignmentType.ToUpper();
            if (assignmentType != "TEMPLATE" && assignmentType != "MESSAGE")
                throw new ArgumentException("AssignmentType must be either 'TEMPLATE' or 'MESSAGE'");

            if (assignmentType == "TEMPLATE" && (!request.DeviceTemplateComboId.HasValue || request.DeviceTemplateComboId <= 0))
                throw new ArgumentException("Valid DeviceTemplateComboId is required for TEMPLATE assignments");

            if (assignmentType == "MESSAGE" && (!request.DeviceMessageComboId.HasValue || request.DeviceMessageComboId <= 0))
                throw new ArgumentException("Valid DeviceMessageComboId is required for MESSAGE assignments");

            if (string.IsNullOrWhiteSpace(request.LocationType))
                throw new ArgumentException("LocationType is required");

            if (request.LocationId <= 0)
                throw new ArgumentException("Valid LocationId is required");

            if (request.UserId <= 0)
                throw new ArgumentException("Valid UserId is required");
        }

        private async Task ValidateComboExistsAsync(CreateAssignmentRequest request)
        {
            bool comboExists = request.AssignmentType.ToUpper() switch
            {
                "TEMPLATE" => await _context.DeviceTemplateCombos
                    .AnyAsync(c => c.Id == request.DeviceTemplateComboId && c.IsActive),
                "MESSAGE" => await _context.DeviceMessageCombos
                    .AnyAsync(c => c.Id == request.DeviceMessageComboId && c.IsActive),
                _ => false
            };

            if (!comboExists)
                throw new NotFoundException($"{request.AssignmentType} combo not found");
        }

        private async Task<DeviceAssignment> GetExistingAssignmentAsync(CreateAssignmentRequest request)
        {
            try
            {
                var query = _context.DeviceAssignment
                              .Where(a => a.LocationType.ToLower() == request.LocationType.ToLower() &&
                                         a.LocationId == request.LocationId &&
                                         a.IsActive);

                if (request.AssignmentType.ToUpper() == "TEMPLATE")
                {
                    query = query.Where(a => a.DeviceTemplateComboId == request.DeviceTemplateComboId);
                }
                else
                {
                    query = query.Where(a => a.DeviceMessageComboId == request.DeviceMessageComboId);
                }

                return await query.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing assignment with ID: {Id}");
                throw;
            }
            }

        private async Task<int> GetNextDisplayOrderAsync(string locationType, long locationId)
        {
            var maxOrder = await _context.DeviceAssignment
                .Where(a => a.LocationType.ToLower() == locationType.ToLower() &&
                           a.LocationId == locationId &&
                           a.IsActive)
                .MaxAsync(a => (int?)a.DisplayOrder);

            return maxOrder ?? 0;
        }

        private async Task<AssignmentDto> GetAssignmentDetailsAsync(long assignmentId)
        {
            var assignment = await _context.DeviceAssignment
                .Where(a => a.Id == assignmentId)
                .FirstOrDefaultAsync();

            if (assignment == null)
                throw new NotFoundException($"Assignment not found with ID: {assignmentId}");

            return await MapToAssignmentDtoAsync(assignment);
        }

        private async Task<AssignmentDto> MapToAssignmentDtoAsync(DeviceAssignment assignment)
        {
            var dto = new AssignmentDto
            {
                Id = assignment.Id,
                AssignmentType = assignment.AssignmentType,
                DeviceTemplateComboId = assignment.DeviceTemplateComboId,
                DeviceMessageComboId = assignment.DeviceMessageComboId,
                LocationType = assignment.LocationType,
                LocationId = assignment.LocationId,
                DisplayOrder = assignment.DisplayOrder,
                IsActive = assignment.IsActive,
                CreatedDate = assignment.CreatedDate,
                CreatedUser = assignment.CreatedUser.ToString()
            };

            // Get combo details
            if (assignment.AssignmentType == "TEMPLATE" && assignment.DeviceTemplateComboId.HasValue)
            {
                dto.Combo = await GetTemplateComboDetailsAsync(assignment.DeviceTemplateComboId.Value);
            }
            else if (assignment.AssignmentType == "MESSAGE" && assignment.DeviceMessageComboId.HasValue)
            {
                dto.Combo = await GetMessageComboDetailsAsync(assignment.DeviceMessageComboId.Value);
            }

            // Get user name
            var user = await _context.Users
                .Where(u => u.Id == assignment.CreatedUser)
                .Select(u => new { u.UserName })
                .FirstOrDefaultAsync();

            dto.CreatedUser = user?.UserName;

            return dto;
        }

        private async Task<DeviceTemplateComboDto> GetTemplateComboDetailsAsync(long comboId)
        {
            var details = await _context.DeviceTemplateCombos
                .Where(c => c.Id == comboId)
                .Join(_context.DeviceMaster,
                    combo => combo.DeviceId,
                    device => device.Id,
                    (combo, device) => new { combo, device })
                .Join(_context.MinewTemplates,
                    x => x.combo.TemplateId,
                    template => template.Id,
                    (x, template) => new DeviceTemplateComboDto
                    {
                        Type = "TEMPLATE",
                        DeviceId = x.device.Id,
                        DeviceName = x.device.Name,
                        DeviceMac = x.device.MACAddress,
                        DeviceHeight = x.device.DeviceScreen.Height,
                        DeviceWidth = x.device.DeviceScreen.Width,
                        TemplateId = template.Id,
                        TemplateName = template.Name,
                        ScreenInch = template.ScreenInch,
                        Color = template.Color,
                        Orientation = template.Orientation
                    })
                .FirstOrDefaultAsync();

            return details ?? new DeviceTemplateComboDto
            {
                Type = "TEMPLATE",
                Error = "Combo details not found"
            };
        }

        private async Task<DeviceMessageComboDto> GetMessageComboDetailsAsync(long comboId)
        {
            var details = await _context.DeviceMessageCombos
                .Where(c => c.Id == comboId)

                .Join(_context.DeviceMaster,
                    combo => combo.DeviceId,
                    device => device.Id,
                    (combo, device) => new { combo, device })

                .Join(_context.MessageMaster,
                    x => x.combo.MessageId,
                    message => message.Id,
                    (x, message) => new { x.combo, x.device, message })

                .Join(_context.ContentType,
                    x => x.message.ContentType,
                    ct => ct.Id,
                    (x, ct) => new DeviceMessageComboDto
                    {
                        Type = "MESSAGE",
                        DeviceId = x.device.Id,
                        DeviceName = x.device.Name,
                        DeviceMac = x.device.MACAddress,

                        MessageId = x.message.Id,
                        MessageTitle = x.message.Title,
                        MessageContent = x.message.ContentData,

                        MessageType = ct.Name,          
                    })
                .FirstOrDefaultAsync();

            return details ?? new DeviceMessageComboDto
            {
                Type = "MESSAGE",
                Error = "Combo details not found"
            };
        }




        //private async Task<dynamic> GetTemplateComboDetailsAsync(long comboId)
        //{
        //    var details = await _context.DeviceTemplateCombos
        //        .Where(c => c.Id == comboId)
        //        .Join(_context.DeviceMaster,
        //            combo => combo.DeviceId,
        //            device => device.Id,
        //            (combo, device) => new { combo, device })
        //        .Join(_context.MinewTemplates,
        //            x => x.combo.TemplateId,
        //            template => template.Id,
        //            (x, template) => new
        //            {
        //                Type = "TEMPLATE",
        //                DeviceId = x.device.Id,
        //                DeviceName = x.device.Name,
        //                DeviceMac = x.device.MACAddress,
        //                DeviceHeight = x.device.ScreenHeight,
        //                DeviceWidth = x.device.ScreenWidth,
        //                TemplateId = template.Id,
        //                TemplateName = template.Name,
        //                ScreenInch = template.ScreenInch,
        //                Color = template.Color,
        //                Orientation = template.Orientation
        //            })
        //        .FirstOrDefaultAsync();

        //    return  details ?? new { Type = "TEMPLATE", Error = "Combo details not found" };
        //}

        //private async Task<dynamic> GetMessageComboDetailsAsync(long comboId)
        //{
        //    var details = await _context.DeviceMessageCombos
        //        .Where(c => c.Id == comboId)
        //        .Join(_context.DeviceMaster,
        //            combo => combo.DeviceId,
        //            device => device.Id,
        //            (combo, device) => new { combo, device })
        //        .Join(_context.MessageMaster,
        //            x => x.combo.MessageId,
        //            message => message.Id,
        //            (x, message) => new
        //            {
        //                Type = "MESSAGE",
        //                DeviceId = x.device.Id,
        //                DeviceName = x.device.Name,
        //                DeviceMac = x.device.MACAddress,
        //                MessageId = message.Id,
        //                MessageTitle = message.Title,
        //                MessageContent = message.ContentData,
        //                MessageType = message.ContentType                    })
        //        .FirstOrDefaultAsync();

        //    return details ?? new { Type = "MESSAGE", Error = "Combo details not found" };
        //}

        private IQueryable<AssignmentViewModel> BuildAssignmentQuery(AssignmentPagedRequest request)
        {
            // Base query for TEMPLATE assignments
            var templateQuery = _context.DeviceAssignment
                .Where(a => a.AssignmentType == "TEMPLATE" && a.DeviceTemplateComboId.HasValue)
                .Join(_context.DeviceTemplateCombos,
                    assignment => assignment.DeviceTemplateComboId.Value,
                    combo => combo.Id,
                    (assignment, combo) => new { assignment, combo })
                .Join(_context.DeviceMaster,
                    x => x.combo.DeviceId,
                    device => device.Id,
                    (x, device) => new { x.assignment, x.combo, device })
                .Join(_context.MinewTemplates,
                    x => x.combo.TemplateId,
                    template => template.Id,
                    (x, template) => new { x.assignment, x.combo, x.device, template })
                .Select(x => new AssignmentViewModel
                {
                    Id = x.assignment.Id,
                    AssignmentType = x.assignment.AssignmentType,
                    DeviceTemplateComboId = x.assignment.DeviceTemplateComboId,
                    DeviceMessageComboId = null,
                    DeviceId = x.device.Id,
                    DeviceName = x.device.Name,
                    DeviceHeight = x.device.DeviceScreen.Height,
                    DeviceWidth = x.device.DeviceScreen.Width,
                    DeviceMac = x.device.MACAddress,
                    TemplateId = x.template.Id,
                    TemplateName = x.template.Name,
                    MessageId = null,
                    MessageTitle = null,
                    MessageContent = null,
                    LocationType = x.assignment.LocationType,
                    LocationId = x.assignment.LocationId,
                    DisplayOrder = x.assignment.DisplayOrder,
                    IsActive = x.assignment.IsActive,
                    CreatedDate = x.assignment.CreatedDate,
                    CreatedUser = x.assignment.CreatedUser.ToString()
                });

            // Base query for MESSAGE assignments
            var messageQuery = _context.DeviceAssignment
                .Where(a => a.AssignmentType == "MESSAGE" && a.DeviceMessageComboId.HasValue)
                .Join(_context.DeviceMessageCombos,
                    assignment => assignment.DeviceMessageComboId.Value,
                    combo => combo.Id,
                    (assignment, combo) => new { assignment, combo })
                .Join(_context.DeviceMaster,
                    x => x.combo.DeviceId,
                    device => device.Id,
                    (x, device) => new { x.assignment, x.combo, device })
                .Join(_context.MessageMaster,
                    x => x.combo.MessageId,
                    message => message.Id,
                    (x, message) => new { x.assignment, x.combo, x.device, message })
                .Select(x => new AssignmentViewModel
                {
                    Id = x.assignment.Id,
                    AssignmentType = x.assignment.AssignmentType,
                    DeviceTemplateComboId = null,
                    DeviceMessageComboId = x.assignment.DeviceMessageComboId,
                    DeviceId = x.device.Id,
                    DeviceName = x.device.Name,
                    DeviceHeight = x.device.DeviceScreen.Height,
                    DeviceWidth = x.device.DeviceScreen.Width,
                    DeviceMac = x.device.MACAddress,
                    TemplateId = null,
                    TemplateName = null,
                    MessageId = x.message.Id,
                    MessageTitle = x.message.Title,
                    MessageContent = x.message.ContentData,
                    LocationType = x.assignment.LocationType,
                    LocationId = x.assignment.LocationId,
                    DisplayOrder = x.assignment.DisplayOrder,
                    IsActive = x.assignment.IsActive,
                    CreatedDate = x.assignment.CreatedDate,
                    CreatedUser = x.assignment.CreatedUser.ToString()
                });

            // Combine queries
            var combinedQuery = templateQuery.Concat(messageQuery);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.AssignmentType))
            {
                combinedQuery = combinedQuery.Where(x => x.AssignmentType == request.AssignmentType.ToUpper());
            }

            if (!string.IsNullOrWhiteSpace(request.LocationType))
            {
                combinedQuery = combinedQuery.Where(x => x.LocationType.ToLower() == request.LocationType.ToLower());
            }

            if (request.LocationId.HasValue)
            {
                combinedQuery = combinedQuery.Where(x => x.LocationId == request.LocationId.Value);
            }

            if (request.DeviceTemplateComboId.HasValue)
            {
                combinedQuery = combinedQuery.Where(x => x.DeviceTemplateComboId == request.DeviceTemplateComboId.Value);
            }

            if (request.DeviceMessageComboId.HasValue)
            {
                combinedQuery = combinedQuery.Where(x => x.DeviceMessageComboId == request.DeviceMessageComboId.Value);
            }

            if (request.IsActive.HasValue)
            {
                combinedQuery = combinedQuery.Where(x => x.IsActive == request.IsActive.Value);
            }

            // Apply search term
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.Trim().ToLower();
                combinedQuery = combinedQuery.Where(x =>
                    (x.DeviceName != null && x.DeviceName.ToLower().Contains(searchTerm)) ||
                    (x.DeviceMac != null && x.DeviceMac.ToLower().Contains(searchTerm)) ||
                    (x.TemplateName != null && x.TemplateName.ToLower().Contains(searchTerm)) ||
                    (x.MessageTitle != null && x.MessageTitle.ToLower().Contains(searchTerm)) ||
                    (x.LocationType != null && x.LocationType.ToLower().Contains(searchTerm))
                );
            }

            return combinedQuery;
        }

        private IQueryable<AssignmentViewModel> ApplyAssignmentSorting(IQueryable<AssignmentViewModel> query, string sortBy, bool sortDescending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return sortDescending
                    ? query.OrderByDescending(a => a.CreatedDate)
                    : query.OrderBy(a => a.CreatedDate);
            }

            return sortBy.ToLower() switch
            {
                "devicename" => sortDescending
                    ? query.OrderByDescending(a => a.DeviceName)
                    : query.OrderBy(a => a.DeviceName),
                "devicemac" => sortDescending
                    ? query.OrderByDescending(a => a.DeviceMac)
                    : query.OrderBy(a => a.DeviceMac),
                "templatename" => sortDescending
                    ? query.OrderByDescending(a => a.TemplateName)
                    : query.OrderBy(a => a.TemplateName),
                "messagetitle" => sortDescending
                    ? query.OrderByDescending(a => a.MessageTitle)
                    : query.OrderBy(a => a.MessageTitle),
                "locationtype" => sortDescending
                    ? query.OrderByDescending(a => a.LocationType)
                    : query.OrderBy(a => a.LocationType),
                "locationid" => sortDescending
                    ? query.OrderByDescending(a => a.LocationId)
                    : query.OrderBy(a => a.LocationId),
                "displayorder" => sortDescending
                    ? query.OrderByDescending(a => a.DisplayOrder)
                    : query.OrderBy(a => a.DisplayOrder),
                "createddate" => sortDescending
                    ? query.OrderByDescending(a => a.CreatedDate)
                    : query.OrderBy(a => a.CreatedDate),
                "createduser" => sortDescending
                    ? query.OrderByDescending(a => a.CreatedUser)
                    : query.OrderBy(a => a.CreatedUser),
                "assignmenttype" => sortDescending
                    ? query.OrderByDescending(a => a.AssignmentType)
                    : query.OrderBy(a => a.AssignmentType),
                _ => sortDescending
                    ? query.OrderByDescending(a => a.CreatedDate)
                    : query.OrderBy(a => a.CreatedDate)
            };
        }

        private async Task ReorderAssignmentsAsync(string locationType, long locationId, int userId)
        {
            var assignments = await _context.DeviceAssignment
                .Where(a => a.LocationType == locationType &&
                           a.LocationId == locationId &&
                           a.IsActive)
                .OrderBy(a => a.DisplayOrder)
                .ToListAsync();

            for (int i = 0; i < assignments.Count; i++)
            {
                assignments[i].DisplayOrder = i + 1;
                assignments[i].UpdatedDate = DateTime.UtcNow;
                assignments[i].UpdatedUser = userId;
            }

            await _context.SaveChangesAsync();
        }

        #endregion

        #region Device Screen
        public async Task<DeviceScreenDto> GetScreenByIdAsync(long id)
        {
            try
            {
                var screen = await _context.DeviceScreens
                    .Include(s => s.ScreenType)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (screen == null)
                    throw new KeyNotFoundException($"Screen with ID {id} not found.");

                var dto = MapToDto(screen);
                dto.DeviceCount = await GetDeviceCountByScreenIdAsync(id);

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting screen by ID: {Id}", id);
                throw;
            }
        }

        public async Task<PagedResult<DeviceScreenDto>> GetPagedAsync(DeviceScreenPagedRequest request)
        {
            try
            {
                var query = _context.DeviceScreens
                    .Include(s => s.ScreenType)
                    .AsQueryable();

                // Apply filters
                if (request.ScreenTypeId.HasValue)
                {
                    query = query.Where(s => s.ScreenTypeId == request.ScreenTypeId.Value);
                }

                if (request.MinWidth.HasValue)
                {
                    query = query.Where(s => s.Width >= request.MinWidth.Value);
                }

                if (request.MaxWidth.HasValue)
                {
                    query = query.Where(s => s.Width <= request.MaxWidth.Value);
                }

                if (request.MinHeight.HasValue)
                {
                    query = query.Where(s => s.Height >= request.MinHeight.Value);
                }

                if (request.MaxHeight.HasValue)
                {
                    query = query.Where(s => s.Height <= request.MaxHeight.Value);
                }

                if (request.MinInch.HasValue)
                {
                    query = query.Where(s => s.Inch >= request.MinInch.Value);
                }

                if (request.MaxInch.HasValue)
                {
                    query = query.Where(s => s.Inch <= request.MaxInch.Value);
                }

                //if (request.IsTouchScreen.HasValue)
                //{
                //    query = query.Where(s => s.IsTouchScreen == request.IsTouchScreen.Value);
                //}

                if (request.IsActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == request.IsActive.Value);
                }

                if (request.CreatedFrom.HasValue)
                {
                    query = query.Where(s => s.CreatedDate >= request.CreatedFrom.Value);
                }

                if (request.CreatedTo.HasValue)
                {
                    query = query.Where(s => s.CreatedDate <= request.CreatedTo.Value);
                }

                // Apply search term if provided
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower();
                    query = query.Where(s =>
                        s.Name.ToLower().Contains(searchTerm) ||
                        (s.ScreenType != null && s.ScreenType.Name.ToLower().Contains(searchTerm)) ||
                        s.Description.ToLower().Contains(searchTerm) ||
                        s.AspectRatio.ToLower().Contains(searchTerm));
                }

                // Get total count first (SQL Server 2008 compatible)
                var totalCount = await query.CountAsync();

                // FOR SQL SERVER 2008: Fetch ALL matching records first
                var allScreens = await query.ToListAsync();

                // Apply sorting in memory
                var sortedScreens = ApplySorting(allScreens, request.SortBy, request.SortDescending);

                // Apply pagination in memory
                var pagedScreens = sortedScreens
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // Map to DTOs and get device counts
                var dtos = new List<DeviceScreenDto>();
                foreach (var screen in pagedScreens)
                {
                    var dto = MapToDto(screen);
                    dto.DeviceCount = await GetDeviceCountByScreenIdAsync(screen.Id);
                    dtos.Add(dto);
                }

                return new PagedResult<DeviceScreenDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
                    SearchTerm = request.SearchTerm
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated screens");
                throw;
            }
        }

        private List<DeviceScreen> ApplySorting(List<DeviceScreen> items, string sortBy, bool sortDescending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return sortDescending
                    ? items.OrderByDescending(s => s.Id).ToList()
                    : items.OrderBy(s => s.Id).ToList();
            }

            return sortBy.ToLower() switch
            {
                "name" => sortDescending
                    ? items.OrderByDescending(s => s.Name).ToList()
                    : items.OrderBy(s => s.Name).ToList(),

                "screentype" => sortDescending
                    ? items.OrderByDescending(s => s.ScreenType.Name).ToList()
                    : items.OrderBy(s => s.ScreenType.Name).ToList(),

                "width" => sortDescending
                    ? items.OrderByDescending(s => s.Width).ToList()
                    : items.OrderBy(s => s.Width).ToList(),

                "height" => sortDescending
                    ? items.OrderByDescending(s => s.Height).ToList()
                    : items.OrderBy(s => s.Height).ToList(),

                "inch" => sortDescending
                    ? items.OrderByDescending(s => s.Inch).ToList()
                    : items.OrderBy(s => s.Inch).ToList(),

                "aspectratio" => sortDescending
                    ? items.OrderByDescending(s => s.AspectRatio).ToList()
                    : items.OrderBy(s => s.AspectRatio).ToList(),

                "createddate" => sortDescending
                    ? items.OrderByDescending(s => s.CreatedDate).ToList()
                    : items.OrderBy(s => s.CreatedDate).ToList(),

                "devicecount" => sortDescending
                    ? items.OrderByDescending(s => s.Devices.Count).ToList()
                    : items.OrderBy(s => s.Devices.Count).ToList(),

                _ => sortDescending
                    ? items.OrderByDescending(s => s.Id).ToList()
                    : items.OrderBy(s => s.Id).ToList()
            };
        }

        public async Task<DeviceScreenDto> CreateScreenAsync(DeviceScreenCreateDto createDto)
        {
            try
            {
                // Validate screen type exists
                var screenTypeExists = await _context.DeviceScreenTypes.AnyAsync(st => st.Id == createDto.ScreenTypeId);
                if (!screenTypeExists)
                    throw new ArgumentException($"Screen type with ID {createDto.ScreenTypeId} not found.");

                // Check for duplicate screen name
                var duplicateExists = await _context.DeviceScreens
                    .AnyAsync(s => s.Name == createDto.Name && s.IsActive);

                if (duplicateExists)
                    throw new ArgumentException($"Screen with name '{createDto.Name}' already exists.");

                // Calculate aspect ratio if not provided
                if (string.IsNullOrWhiteSpace(createDto.AspectRatio))
                {
                    createDto.AspectRatio = CalculateAspectRatio(createDto.Width, createDto.Height);
                }

                var screen = new DeviceScreen
                {
                    Name = createDto.Name,
                    ScreenTypeId = createDto.ScreenTypeId,
                    Width = createDto.Width,
                    Height = createDto.Height,
                    Inch = createDto.Inch,
                    AspectRatio = createDto.AspectRatio,
                    DPI = createDto.DPI,
                    IsActive = createDto.IsActive,
                    Description = createDto.Description,
                    CreatedUser = createDto.CreatedUser,
                    CreatedDate = DateTime.UtcNow
                };

                _context.DeviceScreens.Add(screen);
                await _context.SaveChangesAsync();

                // Reload with includes
                var createdScreen = await _context.DeviceScreens
                    .Include(s => s.ScreenType)
                    .FirstOrDefaultAsync(s => s.Id == screen.Id);

                var dto = MapToDto(createdScreen);
                dto.DeviceCount = 0;

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating screen");
                throw;
            }
        }

        public async Task<DeviceScreenDto> UpdateScreenAsync(DeviceScreenUpdateDto updateDto)
        {
            try
            {
                var screen = await _context.DeviceScreens
                    .Include(s => s.ScreenType)
                    .FirstOrDefaultAsync(s => s.Id == updateDto.Id);

                if (screen == null)
                    throw new KeyNotFoundException($"Screen with ID {updateDto.Id} not found.");

                // Check if screen is in use before allowing certain changes
                var deviceCount = await GetDeviceCountByScreenIdAsync(updateDto.Id);
                if (deviceCount > 0)
                {
                    // Don't allow changing critical dimensions if screen is in use
                    if (updateDto.Width.HasValue && updateDto.Width.Value != screen.Width)
                        throw new InvalidOperationException("Cannot change width while screen is in use by devices.");

                    if (updateDto.Height.HasValue && updateDto.Height.Value != screen.Height)
                        throw new InvalidOperationException("Cannot change height while screen is in use by devices.");

                    if (updateDto.Inch.HasValue && updateDto.Inch.Value != screen.Inch)
                        throw new InvalidOperationException("Cannot change inch size while screen is in use by devices.");
                }

                // Update fields if provided
                if (!string.IsNullOrWhiteSpace(updateDto.Name))
                    screen.Name = updateDto.Name;

                if (updateDto.ScreenTypeId.HasValue)
                    screen.ScreenTypeId = updateDto.ScreenTypeId.Value;

                if (updateDto.Width.HasValue)
                    screen.Width = updateDto.Width.Value;

                if (updateDto.Height.HasValue)
                    screen.Height = updateDto.Height.Value;

                if (updateDto.Inch.HasValue)
                    screen.Inch = updateDto.Inch.Value;

                if (!string.IsNullOrWhiteSpace(updateDto.AspectRatio))
                    screen.AspectRatio = updateDto.AspectRatio;
                else if (updateDto.Width.HasValue || updateDto.Height.HasValue)
                    screen.AspectRatio = CalculateAspectRatio(screen.Width, screen.Height);

                if (updateDto.DPI.HasValue)
                    screen.DPI = updateDto.DPI.Value;

                if (updateDto.IsActive.HasValue)
                    screen.IsActive = updateDto.IsActive.Value;

                if (!string.IsNullOrWhiteSpace(updateDto.Description))
                    screen.Description = updateDto.Description;

                screen.UpdatedUser = updateDto.UpdatedUser;
                screen.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var dto = MapToDto(screen);
                dto.DeviceCount = deviceCount;

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating screen with ID: {Id}", updateDto.Id);
                throw;
            }
        }

        public async Task<bool> DeleteScreenAsync(long id)
        {
            try
            {
                var screen = await _context.DeviceScreens
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (screen == null)
                    throw new KeyNotFoundException($"Screen with ID {id} not found.");

                // Check if screen is in use
                var deviceCount = await GetDeviceCountByScreenIdAsync(id);
                if (deviceCount > 0)
                    throw new InvalidOperationException($"Cannot delete screen. It is currently used by {deviceCount} device(s).");

                _context.DeviceScreens.Remove(screen);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting screen with ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ToggleScreenActiveAsync(long id, int userId)
        {
            try
            {
                var screen = await _context.DeviceScreens
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (screen == null)
                    throw new KeyNotFoundException($"Screen with ID {id} not found.");

                // Check if screen is in use before deactivating
                if (screen.IsActive)
                {
                    var deviceCount = await GetDeviceCountByScreenIdAsync(id);
                    if (deviceCount > 0)
                        throw new InvalidOperationException($"Cannot deactivate screen. It is currently used by {deviceCount} device(s).");
                }

                screen.IsActive = !screen.IsActive;
                screen.UpdatedUser = userId;
                screen.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return screen.IsActive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling active status for screen with ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<DeviceScreenDto>> GetAvailableScreensAsync(long? screenTypeId = null)
        {
            try
            {
                var query = _context.DeviceScreens
                    .Include(s => s.ScreenType)
                    .Where(s => s.IsActive);

                if (screenTypeId.HasValue)
                {
                    query = query.Where(s => s.ScreenTypeId == screenTypeId.Value);
                }

                var screens = await query
                    .OrderBy(s => s.ScreenType.Name)
                    .ThenBy(s => s.Inch)
                    .ThenBy(s => s.Name)
                    .ToListAsync();

                var dtos = new List<DeviceScreenDto>();
                foreach (var screen in screens)
                {
                    var dto = MapToDto(screen);
                    dto.DeviceCount = await GetDeviceCountByScreenIdAsync(screen.Id);
                    dtos.Add(dto);
                }

                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available screens");
                throw;
            }
        }

        public async Task<int> GetDeviceCountByScreenIdAsync(long screenId)
        {
            return await _context.DeviceMaster
                .CountAsync(d => d.ScreenId == screenId);
        }

        public async Task<bool> CanDeleteScreenAsync(long screenId)
        {
            var deviceCount = await GetDeviceCountByScreenIdAsync(screenId);
            return deviceCount == 0;
        }

        // Helper methods
        private DeviceScreenDto MapToDto(DeviceScreen screen)
        {
            return new DeviceScreenDto
            {
                Id = screen.Id,
                Name = screen.Name,
                ScreenTypeId = screen.ScreenTypeId,
                ScreenTypeName = screen.ScreenType?.Name ?? "Unknown",
                Width = screen.Width,
                Height = screen.Height,
                Inch = screen.Inch,
                AspectRatio = screen.AspectRatio,
                DPI = screen.DPI,
                IsActive = screen.IsActive,
                Description = screen.Description,
                CreatedDate = screen.CreatedDate,
                UpdatedDate = screen.UpdatedDate,
                CreatedUser = screen.CreatedUser,
                UpdatedUser = screen.UpdatedUser
            };
        }

        private string CalculateAspectRatio(int width, int height)
        {
            // Calculate greatest common divisor
            int a = width;
            int b = height;

            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }

            int gcd = a;
            return $"{width / gcd}:{height / gcd}";
        }

        public Task<dynamic> AddGatewayToMinewCloudAsync(string token, string mac, string name, string storeId)
        {
            throw new NotImplementedException();
        }

        public Task<dynamic> DeleteGatewayFromMinewCloudAsync(string token, string gatewayId, string storeId)
        {
            throw new NotImplementedException();
        }

        public Task<dynamic> UpdateGatewayInMinewCloudAsync(string token, string gatewayId, string name)
        {
            throw new NotImplementedException();
        }

        #endregion

        // Custom Exceptions
        public class NotFoundException : Exception
        {
            public NotFoundException(string message) : base(message) { }
        }

        public class ValidationException : Exception
        {
            public ValidationException(string message) : base(message) { }
        }
    }
}
