using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Data;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Interface;
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Models.shelf;
using TERMS_LOYALTY_API.Services;
using TERMS_LOYALTY_API.Shared.Enum;
using TERMS_LOYALTY_API.Shared.Helpers;
using TERMS_LOYALTY_API.SignalRHubs;

namespace TERMS_LOYALTY_API.Repository
{
    public class QueueRepository : IQueue
    {
        private readonly SmartShelfDbContext _context;
        private readonly ILogger<QueueRepository> _logger;
        private readonly MinewCloudService _minewService;
        private readonly IHubContext<DeviceAssignmentHub> _hubContext;

        public QueueRepository(SmartShelfDbContext context, ILogger<QueueRepository> logger, MinewCloudService cloudService, IHubContext<DeviceAssignmentHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _minewService = cloudService;
            _hubContext = hubContext;
        }

        //public async Task<PagedResult<QueueDto>> GetQueuesPagedAsync(QueuePagedRequest request)
        //{
        //    try
        //    {
        //        var query = _context.QueueMaster
        //            .Include(q => q.Device)
        //            .Include(q => q.Status)
        //            .Include(q => q.Priority)
        //            .Include(q => q.Store)
        //            .AsQueryable();

        //        // Apply filters
        //        if (request.StoreId.HasValue)
        //            query = query.Where(q => q.StoreId == request.StoreId.Value);

        //        if (!string.IsNullOrEmpty(request.QueueType))
        //            query = query.Where(q => q.QueueType == request.QueueType);

        //        if (!string.IsNullOrEmpty(request.Status))
        //            query = query.Where(q => q.Status.Name == request.Status);

        //        if (request.DeviceId.HasValue)
        //            query = query.Where(q => q.DeviceId == request.DeviceId.Value);

        //        if (!string.IsNullOrEmpty(request.LocationType))
        //            query = query.Where(q => q.LocationType == request.LocationType);

        //        if (request.LocationId.HasValue)
        //            query = query.Where(q => q.LocationId == request.LocationId.Value);

        //        if (request.StartDateFrom.HasValue)
        //            query = query.Where(q => q.StartDate >= request.StartDateFrom.Value);

        //        if (request.StartDateTo.HasValue)
        //            query = query.Where(q => q.StartDate <= request.StartDateTo.Value);

        //        if (request.IsActive.HasValue)
        //            query = query.Where(q => q.IsActive == request.IsActive.Value);

        //        // Search term
        //        if (!string.IsNullOrEmpty(request.SearchTerm))
        //        {
        //            query = query.Where(q =>
        //                q.Device.Name.Contains(request.SearchTerm) ||
        //                q.Device.MACAddress.Contains(request.SearchTerm) ||
        //                q.TemplateId.Contains(request.SearchTerm) ||
        //                q.LocationType.Contains(request.SearchTerm));
        //        }

        //        // Total count for pagination
        //        var totalCount = await query.CountAsync();

        //        // Apply sorting
        //        query = ApplySorting(query, request.SortBy, request.SortDescending);

        //        // Apply pagination
        //        var items = await query
        //            .Skip((request.PageNumber - 1) * request.PageSize)
        //            .Take(request.PageSize)
        //            .Select(q => new QueueDto
        //            {
        //                Id = q.Id,
        //                QueueType = q.QueueType,
        //                DeviceName = q.Device.Name,
        //                DeviceType = q.Device.DeviceType,
        //                LocationType = q.LocationType,
        //                LocationName = GetLocationName(q.LocationType, q.LocationId, q.ProductId, q.ShelfId),
        //                ProductId = q.ProductId,
        //                ProductName = q.ProductId.HasValue ?
        //                    _context.ProductMaster.FirstOrDefault(p => p.Id == q.ProductId).ProductName : null,
        //                ShelfId = q.ShelfId,
        //                ShelfName = q.ShelfId.HasValue ?
        //                    _context.ShelfMaster.FirstOrDefault(s => s.Id == q.ShelfId).Name : null,
        //                TemplateName = q.TemplateId != null ?
        //                    _context.MinewTemplates.FirstOrDefault(t => t.Id == q.TemplateId).Name : null,
        //                MessageTitle = q.MessageId.HasValue ?
        //                    _context.MessageMaster.FirstOrDefault(m => m.Id == q.MessageId).Title : null,
        //                StartDate = q.StartDate,
        //                EndDate = q.EndDate,
        //                Status = q.Status.Name,
        //                Priority = q.Priority.PriorityName,
        //                IsActive = q.IsActive,
        //                IsRecurring = q.IsRecurring,
        //                RecurrencePattern = q.RecurrencePattern,
        //                DisplayOrder = q.DisplayOrder,
        //                CreatedDate = q.CreatedDate,
        //                StoreId = q.StoreId,
        //                StoreName = q.Store.StoreName
        //            })
        //            .ToListAsync();

        //        return new PagedResult<QueueDto>
        //        {
        //            Items = items,
        //            TotalCount = totalCount,
        //            PageNumber = request.PageNumber,
        //            PageSize = request.PageSize,
        //            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting paginated queues");
        //        throw;
        //    }
        //}

        public async Task<PagedResult<QueueDto>> GetQueuesPagedAsync(QueuePagedRequest request)
        {
            try
            {
                var query = _context.QueueMaster
                    .AsNoTracking()
                    .AsQueryable();

                // =====================
                // FILTERS
                // =====================

                if (request.StoreId.HasValue)
                    query = query.Where(q => q.StoreId == request.StoreId.Value);

                if (!string.IsNullOrEmpty(request.QueueType))
                    query = query.Where(q => q.QueueType == request.QueueType);

                if (request.IsActive.HasValue)
                    query = query.Where(q => q.IsActive == request.IsActive.Value);

                // Everything below was declared on QueuePagedRequest but never
                // applied, so the Status dropdown and the date range in Queue
                // Management silently returned every row.
                if (!string.IsNullOrEmpty(request.Status))
                {
                    // The UI sends "COMPLETED" while Status.Name is "Completed".
                    // SQL Server's default collation is case-insensitive, so a
                    // direct comparison matches either way.
                    var status = request.Status.Trim();
                    query = query.Where(q => q.Status != null && q.Status.Name == status);
                }

                if (request.DeviceId.HasValue)
                    query = query.Where(q => q.DeviceId == request.DeviceId.Value);

                if (!string.IsNullOrEmpty(request.LocationType))
                {
                    var locationType = request.LocationType.Trim();
                    query = query.Where(q => q.LocationType == locationType);
                }

                if (request.LocationId.HasValue)
                    query = query.Where(q => q.LocationId == request.LocationId.Value);

                if (request.StartDateFrom.HasValue)
                    query = query.Where(q => q.StartDate >= request.StartDateFrom.Value);

                if (request.StartDateTo.HasValue)
                    query = query.Where(q => q.StartDate <= request.StartDateTo.Value);

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    var term = request.SearchTerm.Trim();

                    query = query.Where(q =>
                        q.Device.Name.Contains(term) ||
                        q.Device.MACAddress.Contains(term) ||
                        q.QueueType.Contains(term) ||
                        q.LocationType.Contains(term));
                }

                var totalCount = await query.CountAsync();

                // =====================
                // SORTING
                // =====================

                query = request.SortBy?.ToLower() switch
                {
                    "devicename" => request.SortDescending
                        ? query.OrderByDescending(q => q.Device.Name)
                        : query.OrderBy(q => q.Device.Name),

                    "startdate" => request.SortDescending
                        ? query.OrderByDescending(q => q.StartDate)
                        : query.OrderBy(q => q.StartDate),

                    "createddate" => request.SortDescending
                        ? query.OrderByDescending(q => q.CreatedDate)
                        : query.OrderBy(q => q.CreatedDate),

                    _ => query.OrderByDescending(q => q.CreatedDate)
                };

                // =====================
                // SQL 2008 SAFE PAGING
                // =====================

                int skip = (request.PageNumber - 1) * request.PageSize;
                int take = request.PageNumber * request.PageSize;

                var allFetched = await query
                    .Take(take)
                    .Select(q => new QueueDto
                    {
                        Id = q.Id,
                        QueueType = q.QueueType,
                        DeviceName = q.Device.Name,
                        DeviceType = q.Device.DeviceType,
                        LocationType = q.LocationType,
                        ProductName = q.Product != null ? q.Product.ProductName : null,
                        ShelfName = q.Shelf != null ? q.Shelf.Name : null,
                        MessageTitle = q.Message != null ? q.Message.Title : null,
                        TemplateName = q.Template != null ? q.Template.Name : null,
                        Status = q.Status != null ? q.Status.Name : "Unknown",
                        Priority = q.Priority.PriorityName,
                        StartDate = q.StartDate,
                        EndDate = q.EndDate,
                        IsActive = q.IsActive,
                        CreatedDate = q.CreatedDate,
                        StoreName = q.Store.StoreName
                    })
                    .ToListAsync();

                var items = allFetched
                    .Skip(skip)
                    .Take(request.PageSize)
                    .ToList();

                return new PagedResult<QueueDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated queues");
                throw;
            }
        }



        //public async Task<QueueDto> CreateDirectQueueAsync(CreateDirectQueueRequest request)
        //{
        //    // Validate device exists
        //    var device = await _context.DeviceMaster.FindAsync(request.DeviceId);
        //    if (device == null)
        //        throw new ArgumentException("Device not found");

        //    // Validate content based on device type
        //    if (device.DeviceType == "Minew" && string.IsNullOrEmpty(request.TemplateId))
        //        throw new ArgumentException("Template is required for Minew devices");

        //    if (device.DeviceType == "Standard" && !request.MessageId.HasValue)
        //        throw new ArgumentException("Message is required for Standard devices");

        //    // Validate location
        //    await ValidateLocationAsync(request.LocationType, request.LocationId);

        //    var queue = new QueueMaster
        //    {
        //        DeviceId = request.DeviceId,
        //        TemplateId = request.TemplateId,
        //        MessageId = request.MessageId,
        //        LocationType = request.LocationType,
        //        LocationId = request.LocationId,
        //        ProductId = request.LocationType == "PRODUCT" ? request.LocationId : (long?)null,
        //        ShelfId = request.LocationType == "SHELF" ? request.LocationId : (long?)null,
        //        StartDate = request.StartDate,
        //        EndDate = request.EndDate,
        //        IsActive = true,
        //        StatusId = (int)QueueStatus.Pending,
        //        PriorityId = request.PriorityId ?? 4, // Default to SCHEDULED
        //        QueueType = request.QueueType ?? (device.DeviceType == "Minew" ? "TEMPLATE_QUEUE" : "MESSAGE_QUEUE"),
        //        DisplayOrder = 1,
        //        IsRecurring = request.IsRecurring,
        //        RecurrencePattern = request.RecurrencePattern,
        //        CreatedDate = DateTime.UtcNow,
        //        CreatedUser = request.UserId,
        //        StoreId = device.StoreId
        //    };

        //    _context.QueueMaster.Add(queue);
        //    await _context.SaveChangesAsync();
        //    return queue.ToDto();

        //    //eturn await GetQueueByIdAsync(queue.Id);
        //}

        public async Task<QueueDto> CreateDirectQueueAsync(CreateDirectQueueRequest request)
        {
            try
            {
                // Validate device exists
                var device = await _context.DeviceMaster.FindAsync(request.DeviceId);
                if (device == null)
                    throw new ArgumentException("Device not found");

                // Validate content based on device type
                if (device.DeviceType == "Minew" && string.IsNullOrEmpty(request.TemplateId))
                    throw new ArgumentException("Template is required for Minew devices");

                if (device.DeviceType == "Standard" && !request.MessageId.HasValue)
                    throw new ArgumentException("Message is required for Standard devices");

                // Validate location
                await ValidateLocationAsync(request.LocationType, request.LocationId);

                // Check if queue already exists for this device and content in the same time period
                var existingQueue = await _context.QueueMaster
                    .Where(q => q.DeviceId == request.DeviceId
                        && q.IsActive == true
                        && q.StatusId != (int)QueueStatus.Completed
                        && q.StatusId != (int)QueueStatus.Failed)
                    .Where(q =>
                        // Check for template-based queues (Minew devices)
                        (!string.IsNullOrEmpty(request.TemplateId) && q.TemplateId == request.TemplateId) ||
                        // Check for message-based queues (Standard devices)
                        (request.MessageId.HasValue && q.MessageId == request.MessageId))
                    .Where(q =>
                        // Check for overlapping schedules
                        ((request.StartDate >= q.StartDate && request.StartDate < q.EndDate) ||  // New start falls within existing queue
                         (request.EndDate > q.StartDate && request.EndDate <= q.EndDate) ||      // New end falls within existing queue
                         (request.StartDate <= q.StartDate && request.EndDate >= q.EndDate))     // New queue completely overlaps existing
                    )
                    .FirstOrDefaultAsync();

                if (existingQueue != null)
                {
                    throw new InvalidOperationException(
                        $"A queue with this {(device.DeviceType == "Minew" ? "template" : "message")} " +
                        $"already exists for the specified device during this time period. " +
                        $"Existing queue runs from {existingQueue.StartDate:yyyy-MM-dd HH:mm} to {existingQueue.EndDate:yyyy-MM-dd HH:mm}");
                }

                var queue = new QueueMaster
                {
                    DeviceId = request.DeviceId,
                    TemplateId = request.TemplateId,
                    MessageId = request.MessageId,
                    // Stored and compared in one canonical casing - see
                    // ExecuteMinewQueue, which resolves the goodsMap from this.
                    LocationType = request.LocationType?.Trim().ToUpperInvariant(),
                    LocationId = request.LocationId,
                    ProductId = request.LocationType?.Trim().ToUpperInvariant() == "PRODUCT"
                        ? request.LocationId : (long?)null,
                    ShelfId = request.LocationType?.Trim().ToUpperInvariant() == "SHELF"
                        ? request.LocationId : (long?)null,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    IsActive = true,
                    StatusId = (int)QueueStatus.Pending,
                    PriorityId = request.PriorityId ?? 4, // Default to SCHEDULED
                    QueueType = request.QueueType ?? (device.DeviceType == "Minew" ? "TEMPLATE_QUEUE" : "MESSAGE_QUEUE"),
                    DisplayOrder = 1,
                    IsRecurring = request.IsRecurring,
                    RecurrencePattern = request.RecurrencePattern,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = request.UserId,
                    StoreId = device.StoreId
                };

                _context.QueueMaster.Add(queue);
                await _context.SaveChangesAsync();

                return queue.ToDto();
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("Failed to create queue due to database error", ex);
            }
            catch (ArgumentException)
            {
                // Re-throw argument exceptions as they are
                throw;
            }
            catch (Exception ex)
            {
                // Log the exception here if you have logging
                // _logger.LogError(ex, "Error creating direct queue for device {DeviceId}", request.DeviceId);

                throw new InvalidOperationException("An error occurred while creating the queue", ex);
            }
        }

        public async Task<QueueDto> CreateQueueFromAssignmentAsync(CreateQueueFromAssignmentRequest request)
        {
            try
            {
                var assignment = await _context.DeviceAssignment
               .Include(a => a.DeviceTemplateCombo)
               .Include(a => a.DeviceMessageCombo)
               .FirstOrDefaultAsync(a => a.Id == request.AssignmentId);

                if (assignment == null)
                    throw new KeyNotFoundException("Assignment not found");

                var deviceId = assignment.DeviceTemplateCombo?.DeviceId ??
                              assignment.DeviceMessageCombo?.DeviceId;

                var device = await _context.DeviceMaster.FindAsync(deviceId);
                if (device == null)
                    throw new KeyNotFoundException("Device not found");

                var queue = new QueueMaster
                {
                    DeviceId = deviceId,
                    TemplateId = assignment.DeviceTemplateCombo?.TemplateId,
                    MessageId = assignment.DeviceMessageCombo?.MessageId,
                    // Normalised on write: ExecuteMinewQueue compares this against
                    // "PRODUCT"/"SHELF", and an assignment stores "Product". The
                    // raw value fell through both branches and bound a placeholder
                    // goodsMap (id "0"), which Minew answers with 数据不存在 -
                    // or accepts, and paints an empty label.
                    LocationType = assignment.LocationType?.Trim().ToUpperInvariant(),
                    LocationId = assignment.LocationId,
                    ProductId = assignment.LocationType?.Trim().ToUpper() == "PRODUCT" ? assignment.LocationId : null,
                    ShelfId = assignment.LocationType?.Trim().ToUpper() == "SHELF" ? assignment.LocationId : null,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    IsActive = true,
                    StatusId = (int)QueueStatus.Pending,
                    PriorityId = request.PriorityId ?? 4,
                    QueueType = assignment.AssignmentType == "TEMPLATE" ? "TEMPLATE_QUEUE" : "MESSAGE_QUEUE",
                    DisplayOrder = request.DisplayOrder ?? assignment.DisplayOrder,
                    IsRecurring = request.IsRecurring,
                    RecurrencePattern = request.RecurrencePattern,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = request.UserId,
                    StoreId = device.StoreId
                };

                _context.QueueMaster.Add(queue);
                await _context.SaveChangesAsync();

                return queue.ToDto();
            }
            catch(Exception ex)
            {
                throw;
            }
           
        }

        public async Task<QueueDetailDto> GetQueueByIdAsync(long id)
        {
            try
            {
                var queue = await _context.QueueMaster
                    .Include(q => q.Device)
                        .ThenInclude(d => d.DeviceScreen)
                    .Include(q => q.Status)
                    .Include(q => q.Priority)
                    .Include(q => q.Store)
                    .FirstOrDefaultAsync(q => q.Id == id);

                if (queue == null)
                    throw new KeyNotFoundException($"Queue with ID {id} not found");

                // Get related data
                var message = queue.MessageId.HasValue ?
                    await _context.MessageMaster.FindAsync(queue.MessageId.Value) : null;

                var template = !string.IsNullOrEmpty(queue.TemplateId) ?
                    await _context.MinewTemplates.FirstOrDefaultAsync(t => t.Id == queue.TemplateId) : null;

                // Get location name
                string locationName = await GetLocationNameAsync(queue.LocationType, queue.LocationId);

                // Get user names
                var createdUser = await _context.Users.FindAsync(queue.CreatedUser);
                var updatedUser = queue.UpdatedUser.HasValue ?
                    await _context.Users.FindAsync(queue.UpdatedUser.Value) : null;

                return new QueueDetailDto
                {
                    Id = queue.Id,
                    QueueType = queue.QueueType,
                    DeviceName = queue.Device?.Name,
                    DeviceType = queue.Device?.DeviceType,
                    DeviceMac = queue.Device?.MACAddress,
                    LocationType = queue.LocationType,
                    LocationName = locationName,
                    ProductId = queue.ProductId,
                    ProductName = queue.ProductId.HasValue ?
                        (await _context.ProductMaster.FindAsync(queue.ProductId.Value))?.ProductName : null,
                    ShelfId = queue.ShelfId,
                    ShelfName = queue.ShelfId.HasValue ?
                        (await _context.ShelfMaster.FindAsync(queue.ShelfId.Value))?.Name : null,
                    TemplateId = queue.TemplateId,
                    TemplateName = template?.Name,
                    MessageId = queue.MessageId,
                    MessageTitle = message?.Title,
                    ContentData = message?.ContentData,
                    FileUrl = message?.FileUrl,
                    Duration = message?.Duration,
                    StartDate = queue.StartDate,
                    EndDate = queue.EndDate,
                    Status = queue.Status?.Name,
                    Priority = queue.Priority?.PriorityName,
                    IsActive = queue.IsActive,
                    IsRecurring = queue?.IsRecurring ?? false,
                    RecurrencePattern = queue.RecurrencePattern,
                    DisplayOrder = queue.DisplayOrder,
                    BindingData = queue.BindingData,
                    ErrorMessage = queue.ErrorMessage,
                    RetryCount = queue?.RetryCount ?? 0,
                    LastAttempt = queue.LastAttempt,
                    CreatedDate = queue.CreatedDate,
                    CreatedBy = createdUser != null ?
                        $"{createdUser.FirstName} {createdUser.LastName}" : "System",
                    UpdatedDate = queue.UpdatedDate,
                    UpdatedBy = updatedUser != null ?
                        $"{updatedUser.FirstName} {updatedUser.LastName}" : null,
                    StoreId = queue.StoreId,
                    StoreName = queue.Store?.StoreName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting queue by ID: {id}");
                throw;
            }
        }

        public async Task<QueueDto> UpdateQueueAsync(long id, UpdateQueueRequest request)
        {
            try
            {
                var queue = await _context.QueueMaster
                    .Include(q => q.Device)
                    .FirstOrDefaultAsync(q => q.Id == id);

                if (queue == null)
                    throw new KeyNotFoundException($"Queue with ID {id} not found");

                // Update allowed fields
                if (request.StartDate.HasValue)
                    queue.StartDate = request.StartDate.Value;

                if (request.EndDate.HasValue)
                    queue.EndDate = request.EndDate.Value;

                if (request.PriorityId.HasValue)
                    queue.PriorityId = request.PriorityId.Value;

                if (request.DisplayOrder.HasValue)
                    queue.DisplayOrder = request.DisplayOrder.Value;

                if (request.IsActive.HasValue)
                    queue.IsActive = request.IsActive.Value;

                if (request.IsRecurring.HasValue)
                    queue.IsRecurring = request.IsRecurring.Value;

                if (!string.IsNullOrEmpty(request.RecurrencePattern))
                    queue.RecurrencePattern = request.RecurrencePattern;

                queue.UpdatedDate = DateTime.UtcNow;
                queue.UpdatedUser = request.UserId;

                // If queue is being deactivated, also deactivate any active display
                if (request.IsActive.HasValue && !request.IsActive.Value &&
                    queue.StatusId == (int)QueueStatus.Completed)
                {
                    await DeactivateQueueDisplay(queue);
                }

                await _context.SaveChangesAsync();

                // Clear cache or notify if needed
                await NotifyQueueUpdate(queue);

                return await GetQueueByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating queue ID: {id}");
                throw;
            }
        }

        public async Task<bool> DeleteQueueAsync(long id)
        {
            try
            {
                var queue = await _context.QueueMaster.FindAsync(id);

                if (queue == null)
                    return false;

                // Check if queue is currently active
                if (queue.StatusId == (int)QueueStatus.Completed &&
                    queue.StartDate <= DateTime.UtcNow &&
                    (queue.EndDate == null || queue.EndDate > DateTime.UtcNow))
                {
                    throw new InvalidOperationException("Cannot delete an active queue. Deactivate it first.");
                }

                // Deactivate any active display
                if (queue.StatusId == (int)QueueStatus.Completed)
                {
                    await DeactivateQueueDisplay(queue);
                }

                _context.QueueMaster.Remove(queue);
                await _context.SaveChangesAsync();

                // Notify removal
                await NotifyQueueRemoval(queue);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting queue ID: {id}");
                throw;
            }
        }

        public async Task<QueueDto> ActivateQueueAsync(long id)
        {
            try
            {
                var queue = await _context.QueueMaster
                    .Include(q => q.Device)
                    .FirstOrDefaultAsync(q => q.Id == id);

                if (queue == null)
                    throw new KeyNotFoundException($"Queue with ID {id} not found");

                if (queue.StatusId == (int)QueueStatus.Completed)
                    throw new InvalidOperationException("Queue is already active");

                if (queue.StartDate > DateTime.UtcNow)
                    throw new InvalidOperationException("Queue start time is in the future");

                // Update status
                queue.StatusId = (int)QueueStatus.Completed;
                queue.UpdatedDate = DateTime.UtcNow;
                queue.LastAttempt = DateTime.UtcNow;

                // Execute the queue based on type
                await ExecuteQueueContent(queue);

                await _context.SaveChangesAsync();

                // Notify activation
                await NotifyQueueActivation(queue);

                return await GetQueueByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error activating queue ID: {id}");

                // Update queue as failed
                var queue = await _context.QueueMaster.FindAsync(id);
                if (queue != null)
                {
                    queue.StatusId = (int)QueueStatus.Failed;
                    queue.ErrorMessage = ex.Message;
                    queue.RetryCount = (queue?.RetryCount ?? 0) + 1;
                    queue.LastAttempt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                throw;
            }
        }

        public async Task<QueueDto> DeactivateQueueAsync(long id)
        {
            try
            {
                var queue = await _context.QueueMaster
                    .Include(q => q.Device)
                    .FirstOrDefaultAsync(q => q.Id == id);

                if (queue == null)
                    throw new KeyNotFoundException($"Queue with ID {id} not found");

                if (queue.StatusId != (int)QueueStatus.Completed)
                    throw new InvalidOperationException("Queue is not active");

                // Retire the queue rather than reverting it to Pending. It has
                // already run, so Completed is the truthful status, and it is
                // what the completed-count statistic keys off.
                //
                // Reverting to Pending caused three problems: the duplicate
                // check on creation skips only Completed/Failed, so a finished
                // queue kept rejecting new ones for the same device+template
                // until its window expired; ProcessPendingQueues re-activates
                // anything Pending whose window is still open, so a manual
                // deactivation silently undid itself within 30 seconds; and
                // finished queues dropped out of the completed statistic.
                //
                // IsActive = false settles all three - every "active",
                // "pending" and "upcoming" query already requires IsActive,
                // and recurrence is unaffected because CreateNextRecurrence
                // inserts a separate row for the next occurrence.
                queue.IsActive = false;
                queue.UpdatedDate = DateTime.UtcNow;

                // Deactivate display
                await DeactivateQueueDisplay(queue);

                await _context.SaveChangesAsync();

                // Notify deactivation
                await NotifyQueueDeactivation(queue);

                return await GetQueueByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deactivating queue ID: {id}");
                throw;
            }
        }

        public async Task<List<QueueDto>> GetUpcomingQueuesAsync(int hours = 24)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddHours(hours);

                var queues = await _context.QueueMaster
                    .Include(q => q.Device)
                    .Include(q => q.Status)
                    .Include(q => q.Priority)
                    .Include(q => q.Store)
                    .Where(q => q.IsActive &&
                               q.StatusId == (int)QueueStatus.Pending &&
                               q.StartDate <= cutoffTime &&
                               q.StartDate > DateTime.UtcNow)
                    .OrderBy(q => q.StartDate)
                    .Take(100) // Limit to 100 upcoming queues
                    .Select(q => new QueueDto
                    {
                        Id = q.Id,
                        QueueType = q.QueueType,
                        DeviceName = q.Device.Name,
                        DeviceType = q.Device.DeviceType,
                        LocationType = q.LocationType,
                        LocationName = GetLocationName(q.LocationType, q.LocationId, q.ProductId, q.ShelfId),
                        StartDate = q.StartDate,
                        EndDate = q.EndDate,
                        Status = q.Status != null ? q.Status.Name : "Unknown",
                        Priority = q.Priority.PriorityName,
                        IsActive = q.IsActive,
                        DisplayOrder = q.DisplayOrder,
                        StoreName = q.Store.StoreName
                    })
                    .ToListAsync();

                return queues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming queues");
                throw;
            }
        }

        public async Task<List<QueueDto>> GetActiveQueuesAsync()
        {
            try
            {
                var now = DateTime.UtcNow;

                var queues = await _context.QueueMaster
                    .Include(q => q.Device)
                    .Include(q => q.Status)
                    .Include(q => q.Priority)
                    .Include(q => q.Store)
                    .Where(q => q.IsActive &&
                               q.StatusId == (int)QueueStatus.Completed &&
                               q.StartDate <= now &&
                               (q.EndDate == null || q.EndDate > now))
                    .OrderByDescending(q => q.StartDate)
                    .Select(q => new QueueDto
                    {
                        Id = q.Id,
                        QueueType = q.QueueType,
                        DeviceName = q.Device.Name,
                        DeviceType = q.Device.DeviceType,
                        LocationType = q.LocationType,
                        LocationName = GetLocationName(q.LocationType, q.LocationId, q.ProductId, q.ShelfId),
                        StartDate = q.StartDate,
                        EndDate = q.EndDate,
                        Status = q.Status != null ? q.Status.Name : "Unknown",
                        Priority = q.Priority.PriorityName,
                        IsActive = q.IsActive,
                        DisplayOrder = q.DisplayOrder,
                        StoreName = q.Store.StoreName
                    })
                    .ToListAsync();

                return queues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active queues");
                throw;
            }
        }

        public async Task<List<QueueDto>> GetQueuesByDeviceAsync(long deviceId)
        {
            try
            {
                var queues = await _context.QueueMaster
                    .Include(q => q.Device)
                    .Include(q => q.Status)
                    .Include(q => q.Priority)
                    .Include(q => q.Store)
                    .Where(q => q.DeviceId == deviceId)
                    .OrderByDescending(q => q.StartDate)
                    .Take(50) // Limit to 50 most recent queues per device
                    .Select(q => new QueueDto
                    {
                        Id = q.Id,
                        QueueType = q.QueueType,
                        DeviceName = q.Device.Name,
                        DeviceType = q.Device.DeviceType,
                        LocationType = q.LocationType,
                        LocationName = GetLocationName(q.LocationType, q.LocationId, q.ProductId, q.ShelfId),
                        StartDate = q.StartDate,
                        EndDate = q.EndDate,
                        Status = q.Status != null ? q.Status.Name : "Unknown",
                        Priority = q.Priority.PriorityName,
                        IsActive = q.IsActive,
                        DisplayOrder = q.DisplayOrder,
                        StoreName = q.Store.StoreName
                    })
                    .ToListAsync();

                return queues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting queues for device: {deviceId}");
                throw;
            }
        }

        public async Task<List<QueueDto>> GetQueuesByLocationAsync(string locationType, long locationId)
        {
            try
            {
                var queues = await _context.QueueMaster
                    .Include(q => q.Device)
                    .Include(q => q.Status)
                    .Include(q => q.Priority)
                    .Include(q => q.Store)
                    .Where(q => q.LocationType == locationType && q.LocationId == locationId)
                    .OrderByDescending(q => q.StartDate)
                    .Take(50) // Limit to 50 most recent queues per location
                    .Select(q => new QueueDto
                    {
                        Id = q.Id,
                        QueueType = q.QueueType,
                        DeviceName = q.Device.Name,
                        DeviceType = q.Device.DeviceType,
                        LocationType = q.LocationType,
                        LocationName = GetLocationName(q.LocationType, q.LocationId, q.ProductId, q.ShelfId),
                        StartDate = q.StartDate,
                        EndDate = q.EndDate,
                        Status = q.Status != null ? q.Status.Name : "Unknown",
                        Priority = q.Priority.PriorityName,
                        IsActive = q.IsActive,
                        DisplayOrder = q.DisplayOrder,
                        StoreName = q.Store.StoreName
                    })
                    .ToListAsync();

                return queues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting queues for location: {locationType}/{locationId}");
                throw;
            }
        }

        public async Task<object> GetQueueStatisticsAsync(long? storeId = null)
        {
            try
            {
                double avgDuration = 0;

                var query = _context.QueueMaster.AsQueryable();

                if (storeId.HasValue)
                    query = query.Where(q => q.StoreId == storeId.Value);

                var now = DateTime.UtcNow;

                var total = await query.CountAsync();
                var active = await query.CountAsync(q =>
                    q.IsActive &&
                    q.StatusId == (int)QueueStatus.Completed &&
                    q.StartDate <= now &&
                    (q.EndDate == null || q.EndDate > now));

                var pending = await query.CountAsync(q =>
                    q.IsActive &&
                    q.StatusId == (int)QueueStatus.Pending &&
                    q.StartDate > now);

                var completed = await query.CountAsync(q =>
                    q.StatusId == (int)QueueStatus.Completed);

                var failed = await query.CountAsync(q =>
                    q.StatusId == (int)QueueStatus.Failed);

                // Get queue type distribution
                var typeDistribution = await query
                    .GroupBy(q => q.QueueType)
                    .Select(g => new
                    {
                        Type = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                // Get upcoming in next 24 hours
                var upcoming24h = await query.CountAsync(q =>
                    q.IsActive &&
                    q.StatusId == (int)QueueStatus.Pending &&
                    q.StartDate <= now.AddHours(24) &&
                    q.StartDate > now);

                // Get average queue duration
                var durationsQuery = query
                    .Where(q => q.EndDate != null);

                if (await durationsQuery.AnyAsync())
                {
                    avgDuration = await durationsQuery
                        .AverageAsync(q => EF.Functions.DateDiffMinute(q.StartDate, q.EndDate.Value));
                }


                return new
                {
                    Total = total,
                    Active = active,
                    Pending = pending,
                    Completed = completed,
                    Failed = failed,
                    Upcoming24h = upcoming24h,
                    AverageDurationMinutes = Math.Round(avgDuration, 1),
                    TypeDistribution = typeDistribution,
                    SuccessRate = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue statistics");
                throw;
            }
        }

        // ============ PRIVATE HELPER METHODS ============

        private async Task ExecuteQueueContent(QueueMaster queue)
        {
            // Every attempt is recorded, pass or fail. QueueMaster only carries a
            // single LastAttempt/StatusId that each run overwrites, so without
            // this there was no history to explain why a label never changed.
            var startedAt = DateTime.UtcNow;

            try
            {
                if (queue.Device == null)
                    throw new InvalidOperationException("Device not found for queue");

                switch (queue.Device.DeviceType)
                {
                    case "Minew":
                        await ExecuteMinewQueue(queue);
                        break;

                    case "Standard":
                        await ExecuteStandardQueue(queue);
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported device type: {queue.Device.DeviceType}");
                }

                AddExecutionLog(queue, startedAt, (int)QueueStatus.Completed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing queue {queue.Id} content");
                AddExecutionLog(queue, startedAt, (int)QueueStatus.Failed);
                throw;
            }
        }

        /// <summary>
        /// Queues a QueueExecutionLog row for the current attempt. Added to the
        /// change tracker only - the caller's SaveChangesAsync persists it, and
        /// on the failure path the caller's catch does the saving.
        /// </summary>
        private void AddExecutionLog(QueueMaster queue, DateTime startedAt, int statusId)
        {
            try
            {
                var isMessage = string.Equals(queue.QueueType, "MESSAGE_QUEUE",
                    StringComparison.OrdinalIgnoreCase);

                // TargetId is an int column; template ids are Minew's 19-digit
                // snowflakes, so store 0 rather than overflow it.
                var targetId = 0;
                if (isMessage && queue.MessageId.HasValue)
                    targetId = (int)queue.MessageId.Value;

                _context.QueueExecutionLog.Add(new QueueExecutionLog
                {
                    QueueEntryId = (int)queue.Id,
                    QueueTargetTypeId = (int)(isMessage
                        ? QueueTargetType.Message
                        : QueueTargetType.Template),
                    TargetId = targetId,
                    DisplayStartTime = startedAt,
                    DisplayEndTime = DateTime.UtcNow,
                    ActualDuration = (int)(DateTime.UtcNow - startedAt).TotalMilliseconds,
                    StatusId = statusId,
                    CreatedDate = DateTime.UtcNow,
                });
            }
            catch (Exception ex)
            {
                // Logging must never be the reason an execution fails.
                _logger.LogWarning(ex, "Could not record execution log for queue {QueueId}", queue.Id);
            }
        }

        private async Task ExecuteMinewQueue(QueueMaster queue)
        {
            if (string.IsNullOrEmpty(queue.TemplateId))
                throw new InvalidOperationException("Template ID is required for Minew queue");

            var device = queue.Device;
            var store = await _context.StoreMaster.FindAsync(device.StoreId);

            if (store == null)
                throw new InvalidOperationException("Store not found for device");

            // Case-insensitive: queues created from an assignment carry the
            // assignment's own casing ("Product"), and a case-sensitive compare
            // silently skipped the real data.
            var locationType = queue.LocationType?.Trim().ToUpperInvariant();

            Dictionary<string, string> goodsMap = null;
            if (locationType == "PRODUCT" && queue.ProductId.HasValue)
            {
                var product = await _context.ProductMaster.FindAsync(queue.ProductId.Value);
                if (product != null)
                {
                    goodsMap = GenerateGoodsMapFromProduct(product);
                }
            }
            else if (locationType == "SHELF" && queue.ShelfId.HasValue)
            {
                var shelf = await _context.ShelfMaster.FindAsync(queue.ShelfId.Value);
                if (shelf != null)
                {
                    goodsMap = GenerateShelfGoodsMap(shelf);
                }
            }

            // Binding a placeholder pushes id "0" with a 0.00 price, which Minew
            // either rejects (数据不存在) or renders as an empty label. Either way
            // it is never what the operator asked for, so fail loudly instead.
            if (goodsMap == null)
            {
                throw new InvalidOperationException(
                    $"Queue {queue.Id} has no bindable content: locationType " +
                    $"'{queue.LocationType}' with productId {queue.ProductId?.ToString() ?? "null"} " +
                    $"and shelfId {queue.ShelfId?.ToString() ?? "null"} resolved to nothing.");
            }

            // A Minew queue can carry a message alongside its template - the message
            // image rides in goodsMap["image"], same as the manual bind-unified path.
            if (queue.MessageId.HasValue && queue.MessageId.Value > 0)
            {
                var message = await _context.MessageMaster
                    .FirstOrDefaultAsync(m => m.Id == queue.MessageId.Value && m.IsActive);

                if (message != null && IsValidBase64(message.ContentData))
                {
                    // Sent exactly as stored, data URI prefix included: that is
                    // what the known-working bind does, and Minew renders it.
                    // Stripping the prefix is NOT an improvement here.
                    goodsMap["image"] = message.ContentData;
                }
            }

            // Get token and call Minew API
            var token = await GetToken();

            var bindRequest = new
            {
                storeId = store.MinewStoreId,
                labelMac = device.MACAddress,
                goodsMap = goodsMap,
                demoIdMap = new Dictionary<string, string> { ["A"] = queue.TemplateId },
                color = 1,
                total = 5,
                period = 500,
                interval = 900,
                brightness = 100,
                opCode = new Random().Next(1000000000, 2000000000)
            };

            var response = await _minewService.BindData(token, bindRequest);

            // JsonDocument does not override ToString(), so the old assignment
            // stored the literal "System.Text.Json.JsonDocument" and discarded
            // Minew's actual answer - leaving no way to tell whether a queue had
            // really written to the label.
            var rawBindResponse = response?.RootElement.GetRawText() ?? string.Empty;
            queue.BindingData = rawBindResponse;
            queue.LastAttempt = DateTime.UtcNow;

            // Minew reports failure in the body, not the HTTP status. Unchecked,
            // a rejected bind still left the queue marked Completed. Throwing
            // here lets the caller's catch record it as Failed with the reason.
            if (!MinewBindSucceeded(rawBindResponse, out var minewMessage))
            {
                throw new InvalidOperationException(
                    $"Minew rejected the bind: {minewMessage}");
            }
        }

        /// <summary>
        /// True when a raw Minew response carries code 200.
        /// </summary>
        private static bool MinewBindSucceeded(string rawResponse, out string message)
        {
            message = null;
            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                message = "empty response";
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(rawResponse);

                if (doc.RootElement.TryGetProperty("msg", out var msgEl))
                    message = msgEl.GetString();
                else if (doc.RootElement.TryGetProperty("message", out var msgEl2))
                    message = msgEl2.GetString();

                if (doc.RootElement.TryGetProperty("code", out var codeEl))
                {
                    if (codeEl.ValueKind == JsonValueKind.Number && codeEl.TryGetInt32(out var c))
                        return c == 200;
                    if (codeEl.ValueKind == JsonValueKind.String &&
                        int.TryParse(codeEl.GetString(), out var cs))
                        return cs == 200;
                }

                // No code field - do not block on a shape we do not recognise.
                return true;
            }
            catch (JsonException)
            {
                message = "unreadable response";
                return false;
            }
        }

        /// <summary>
        /// Minew wants the picture as a bare base64 string; messages are stored
        /// as data URIs, and sending the prefix leaves the label imageless.
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

                byte[] data = Convert.FromBase64String(base64String);

                // At least 100 bytes for a valid image
                return data.Length > 100;
            }
            catch
            {
                return false;
            }
        }

        private async Task ExecuteStandardQueue(QueueMaster queue)
        {
            if (!queue.MessageId.HasValue)
                throw new InvalidOperationException("Message ID is required for Standard queue");

            var message = await _context.MessageMaster.FindAsync(queue.MessageId.Value);
            if (message == null)
                throw new InvalidOperationException("Message not found");

            var device = queue.Device;

            // Notify via SignalR. SignalR group names are case-sensitive and
            // DeviceAssignmentHub.JoinDeviceGroup lower-cases the device type,
            // so the sender has to lower-case it too - otherwise this went to
            // "assignments-Standard-2" while the clients sat in
            // "assignments-standard-2" and no device ever saw the queue.
            var groupName = DeviceAssignmentHub.GetGroupName(device.DeviceType, device.StoreId);

            await _hubContext.Clients.Group(groupName).SendAsync("QueueActivated", new
            {
                QueueId = queue.Id,
                DeviceId = device.Id,
                DeviceName = device.Name,
                Message = new
                {
                    message.Id,
                    message.Title,
                    message.ContentType,
                    message.ContentData,
                    message.FileUrl,
                    message.Duration
                },
                LocationType = queue.LocationType,
                LocationId = queue.LocationId,
                StartDate = queue.StartDate,
                EndDate = queue.EndDate
            });

            queue.LastAttempt = DateTime.UtcNow;
        }

        private async Task DeactivateQueueDisplay(QueueMaster queue)
        {
            try
            {
                if (queue.Device == null)
                    return;

                switch (queue.Device.DeviceType)
                {
                    case "Minew":
                        // For Minew, we could send a blank template or default display
                        // This depends on your Minew API capabilities
                        break;

                    case "Standard":
                        // Notify via SignalR to clear display
                        var groupName = DeviceAssignmentHub.GetGroupName(queue.Device.DeviceType, queue.Device.StoreId);
                        await _hubContext.Clients.Group(groupName).SendAsync("QueueDeactivated", new
                        {
                            QueueId = queue.Id,
                            DeviceId = queue.Device.Id
                        });
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deactivating display for queue {queue.Id}");
                // Don't throw - this is a cleanup operation
            }
        }

        private async Task NotifyQueueUpdate(QueueMaster queue)
        {
            try
            {
                var groupName = $"queues-updates-{queue.StoreId}";
                await _hubContext.Clients.Group(groupName).SendAsync("QueueUpdated", new
                {
                    QueueId = queue.Id,
                    Status = ((QueueStatus)queue.StatusId).ToString(),
                    IsActive = queue.IsActive,
                    UpdatedDate = queue.UpdatedDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error notifying queue update: {queue.Id}");
            }
        }

        private async Task NotifyQueueActivation(QueueMaster queue)
        {
            try
            {
                var groupName = $"queues-updates-{queue.StoreId}";
                await _hubContext.Clients.Group(groupName).SendAsync("QueueActivated", new
                {
                    QueueId = queue.Id,
                    DeviceId = queue.DeviceId,
                    StartDate = queue.StartDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error notifying queue activation: {queue.Id}");
            }
        }

        private async Task NotifyQueueDeactivation(QueueMaster queue)
        {
            try
            {
                var groupName = $"queues-updates-{queue.StoreId}";
                await _hubContext.Clients.Group(groupName).SendAsync("QueueDeactivated", new
                {
                    QueueId = queue.Id,
                    DeviceId = queue.DeviceId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error notifying queue deactivation: {queue.Id}");
            }
        }

        private async Task NotifyQueueRemoval(QueueMaster queue)
        {
            try
            {
                var groupName = $"queues-updates-{queue.StoreId}";
                await _hubContext.Clients.Group(groupName).SendAsync("QueueDeleted", new
                {
                    QueueId = queue.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error notifying queue removal: {queue.Id}");
            }
        }

        private Dictionary<string, string> GenerateGoodsMapFromProduct(ProductMaster product)
        {
            return new Dictionary<string, string>
            {
                ["id"] = product.Id.ToString(),
                ["specification"] = "2.9",
                ["unit"] = "001f",
                ["price"] = product.SellingPrice.ToString("0.00"),
                ["memberPrice"] = "",
                ["origin"] = "",
                ["discount"] = product.DiscountPrice.ToString("0.00"),
                ["barcoode"] = product.BarCode ?? "",
                ["qrcode"] = "",
                ["p_name"] = product.ProductName,
                ["p_code"] = product.ProductCode ?? ""
            };
        }

        private Dictionary<string, string> GenerateShelfGoodsMap(ShelfMaster shelf)
        {
            return new Dictionary<string, string>
            {
                ["id"] = $"S-{shelf.Id}",
                ["specification"] = "2.9",
                ["unit"] = "001f",
                ["price"] = "0.00",
                ["memberPrice"] = "",
                ["origin"] = "",
                ["discount"] = "0.00",
                ["barcoode"] = "",
                ["qrcode"] = "",
                ["p_name"] = shelf.Name ?? "Shelf Display",
                ["p_code"] = shelf.Id.ToString()
            };
        }

        private async Task<string> GetToken()
        {
            try
            {
                return await _minewService.GetValidTokenAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get Minew token");
                throw new InvalidOperationException("Failed to authenticate with Minew cloud");
            }
        }
        private async Task ValidateLocationAsync(string locationType, long locationId)
        {
            // Accept any casing; callers send "Product" as readily as "PRODUCT".
            switch (locationType?.Trim().ToUpperInvariant())
            {
                case "PRODUCT":
                    var product = await _context.ProductMaster.FindAsync(locationId);
                    if (product == null || !product.IsActive)
                        throw new ArgumentException("Product not found or inactive");
                    break;

                case "SHELF":
                    var shelf = await _context.ShelfMaster.FindAsync(locationId);
                    if (shelf == null || !shelf.IsActive)
                        throw new ArgumentException("Shelf not found or inactive");
                    break;

                default:
                    throw new ArgumentException($"Invalid location type: {locationType}");
            }
        }

        private async Task<string> GetLocationNameAsync(string locationType, long? locationId)
        {
            if (!locationId.HasValue)
                return "Unknown Location";

            // Any casing - an assignment-sourced queue stores "Product".
            switch (locationType?.Trim().ToUpperInvariant())
            {
                case "PRODUCT":
                    var product = await _context.ProductMaster.FindAsync(locationId.Value);
                    return product?.ProductName ?? "Unknown Product";

                case "SHELF":
                    var shelf = await _context.ShelfMaster.FindAsync(locationId.Value);
                    return shelf?.Name ?? "Unknown Shelf";

                case "AISLE":
                    var aisle = await _context.AisleMaster.FindAsync(locationId.Value);
                    return aisle?.Name ?? "Unknown Aisle";

                default:
                    return "Unknown Location";
            }
        }

        private string GetLocationName(string locationType, long? locationId, long? productId, long? shelfId)
        {
            if (locationType == "PRODUCT" && productId.HasValue)
            {
                var product = _context.ProductMaster.Find(productId.Value);
                return product?.ProductName ?? "Unknown Product";
            }

            if (locationType == "SHELF" && shelfId.HasValue)
            {
                var shelf = _context.ShelfMaster.Find(shelfId.Value);
                return shelf?.Name ?? "Unknown Shelf";
            }

            return "Unknown Location";
        }

        private IQueryable<QueueMaster> ApplySorting(IQueryable<QueueMaster> query, string sortBy, bool sortDescending)
        {
            return (sortBy?.ToLower(), sortDescending) switch
            {
                ("startdate", false) => query.OrderBy(q => q.StartDate),
                ("startdate", true) => query.OrderByDescending(q => q.StartDate),
                ("priority", false) => query.OrderBy(q => q.Priority.Value),
                ("priority", true) => query.OrderByDescending(q => q.Priority.Value),
                ("displayorder", false) => query.OrderBy(q => q.DisplayOrder),
                ("displayorder", true) => query.OrderByDescending(q => q.DisplayOrder),
                ("createddate", false) => query.OrderBy(q => q.CreatedDate),
                ("createddate", true) => query.OrderByDescending(q => q.CreatedDate),
                _ => query.OrderByDescending(q => q.CreatedDate) // Default
            };
        }

        public Task<List<PriorityMaster>> GetAllPriorities()
        {
            return _context.PriorityMaster
                .Where(p => p.IsActive)
                .OrderBy(p => p.Value)
                .ToListAsync();
        }
    }
}
