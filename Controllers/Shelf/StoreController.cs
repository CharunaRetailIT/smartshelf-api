using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Interface;
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Models.shelf;
using TERMS_LOYALTY_API.Repository;
using TERMS_LOYALTY_API.Services;
using TERMS_MOBILE_WEB_API.Models;
using static TERMS_LOYALTY_API.DTOs.shelf.MinewStore;

namespace TERMS_LOYALTY_API.Controllers.Shelf
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class StoreController : ControllerBase
    {
        private readonly IStore _storeRepo;
        private readonly ILogger<StoreController> _logger;
        private readonly MinewCloudService _minewService;


        public StoreController(IStore storeRepo, MinewCloudService minewService, ILogger<StoreController> logger)
        {
            _storeRepo = storeRepo;
            _minewService = minewService;
            _logger = logger;
        }


        /// <summary>
        /// Retrieves stores with server-side pagination and filtering
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(HttpResponseData<PagedResult<object>>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetStores([FromQuery] StoreFilterDto filter)
        {
            var response = new HttpResponseData<PagedResult<object>>();
            try
            {
                var result = await _storeRepo.GetStoreDetailsAsync(filter);

                response.Success = true;
                response.Message = "Stores retrieved successfully.";
                response.Result = result;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stores");
                response.Success = false;
                response.Message = "Failed to retrieve stores.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Retrieves a specific store by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(HttpResponseData<StoreMaster>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetStore(long id)
        {
            var response = new HttpResponseData<StoreMaster>();
            try
            {
                var store = await _storeRepo.GetStoreByIdAsync(id);
                if (store == null)
                {
                    response.Success = false;
                    response.Message = "Store not found.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                response.Success = true;
                response.Message = "Store retrieved successfully.";
                response.Result = store;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving store with ID: {Id}", id);
                response.Success = false;
                response.Message = "Failed to retrieve store.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Creates a new store
        /// </summary>
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ProducesResponseType(typeof(HttpResponseData<StoreMaster>), 201)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> CreateStore([FromBody] StoreDto storeDto)
        {
            var response = new HttpResponseData<StoreMaster>();
            try
            {
                if (!ModelState.IsValid)
                {
                    response.Success = false;
                    response.Message = "Invalid store data.";
                    response.ResponsCode = 400;
                    return BadRequest(response);
                }

                // Check if store code already exists
                if (!string.IsNullOrEmpty(storeDto.StoreCode))
                {
                    var codeExists = await _storeRepo.StoreCodeExistsAsync(storeDto.StoreCode);
                    if (codeExists)
                    {
                        response.Success = false;
                        response.Message = "Store code already exists.";
                        response.ResponsCode = 400;
                        return BadRequest(response);
                    }
                }

                var store = new StoreMaster
                {
                    StoreName = storeDto.StoreName,
                    StoreCode = storeDto.StoreCode,
                    Address = storeDto.Address,
                    Phone = storeDto.Phone,
                    Email = storeDto.Email,
                    ContactPerson = storeDto.ContactPerson,
                    StoreType = storeDto.StoreType,
                    MinewStoreId = storeDto.MinewStoreId,
                    LegacyStoreId = null,
                    Latitude = storeDto.Latitude,
                    Longitude = storeDto.Longitude,
                    IsActive = storeDto.IsActive,
                    IsSynced = storeDto.StoreType == "minew" ? false : true,
                    SyncStatus = storeDto.StoreType == "minew" ? "pending" : "not_required",
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = (int)storeDto.CreatedUser
                };

                var createdStore = await _storeRepo.CreateStoreAsync(store);

                // If it's a Minew store, try to sync to cloud. A failure here does
                // not fail the create - the row exists locally either way - but it
                // has to be reported, or a store that never reached Minew looks
                // indistinguishable from one that did.
                string syncNote = string.Empty;
                if (store.StoreType == "minew")
                {
                    try
                    {
                        await SyncStoreToCloud(store.Id);
                        // Re-read so the caller sees the cloud id that sync assigned.
                        createdStore = await _storeRepo.GetStoreByIdAsync(store.Id) ?? createdStore;
                    }
                    catch (Exception syncEx)
                    {
                        _logger.LogWarning(syncEx, "Failed to sync store to cloud during creation");
                        syncNote = $" Cloud sync failed: {syncEx.Message}";
                    }
                }

                response.Success = true;
                response.Message = "Store created successfully." + syncNote;
                response.Result = createdStore;
                response.ResponsCode = 201;
                return CreatedAtAction(nameof(GetStore), new { id = createdStore.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating store");
                response.Success = false;
                response.Message = "Failed to create store.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Updates an existing store
        /// </summary>
        [Authorize(Roles = "Admin,Manager")]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(HttpResponseData<StoreMaster>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> UpdateStore(long id, [FromBody] StoreDto storeDto)
        {
            var response = new HttpResponseData<StoreMaster>();
            try
            {
                if (!ModelState.IsValid)
                {
                    response.Success = false;
                    response.Message = "Invalid store data.";
                    response.ResponsCode = 400;
                    return BadRequest(response);
                }

                var store = await _storeRepo.GetStoreByIdAsync(id);
                if (store == null)
                {
                    response.Success = false;
                    response.Message = "Store not found.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                // Check if store code already exists (excluding current store)
                if (!string.IsNullOrEmpty(storeDto.StoreCode) && storeDto.StoreCode != store.StoreCode)
                {
                    var codeExists = await _storeRepo.StoreCodeExistsAsync(storeDto.StoreCode, id);
                    if (codeExists)
                    {
                        response.Success = false;
                        response.Message = "Store code already exists.";
                        response.ResponsCode = 400;
                        return BadRequest(response);
                    }
                }

                store.StoreName = storeDto.StoreName;
                store.StoreCode = storeDto.StoreCode;
                store.Address = storeDto.Address;
                store.Phone = storeDto.Phone;
                store.Email = storeDto.Email;
                store.ContactPerson = storeDto.ContactPerson;
                store.StoreType = storeDto.StoreType;
                store.Latitude = storeDto.Latitude;
                store.Longitude = storeDto.Longitude;
                store.IsActive = storeDto.IsActive;
                store.UpdatedDate = DateTime.UtcNow;
                store.UpdatedUser = storeDto.CreatedUser;

                // If changing to minew store type, mark for sync
                if (storeDto.StoreType == "minew" && store.StoreType != "minew")
                {
                    store.IsSynced = false;
                    store.SyncStatus = "pending";
                }

                var updatedStore = await _storeRepo.UpdateStoreAsync(store);

                // Sync to cloud if it's a Minew store
                if (store.StoreType == "minew" && !store.IsSynced)
                {
                    try
                    {
                        await SyncStoreToCloud(store.Id);
                    }
                    catch (Exception syncEx)
                    {
                        _logger.LogWarning(syncEx, "Failed to sync store to cloud during update");
                    }
                }

                response.Success = true;
                response.Message = "Store updated successfully.";
                response.Result = updatedStore;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating store with ID: {Id}", id);
                response.Success = false;
                response.Message = "Failed to update store.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Deletes a store (soft delete)
        /// </summary>
        [Authorize(Roles = "Admin,Manager")]
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> DeleteStore(long id)
        {
            var response = new HttpResponseData<object>();
            try
            {
                var store = await _storeRepo.GetStoreByIdAsync(id);
                if (store == null)
                {
                    response.Success = false;
                    response.Message = "Store not found.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                // Check if store has active devices (you might want to add this check)
                // var deviceCount = await _deviceRepository.GetDeviceCountByStoreIdAsync(id);
                // if (deviceCount > 0)
                // {
                //     response.Success = false;
                //     response.Message = "Cannot delete store with active devices.";
                //     response.ResponsCode = 400;
                //     return BadRequest(response);
                // }

                var result = await _storeRepo.DeleteStoreAsync(id);
                if (!result)
                {
                    response.Success = false;
                    response.Message = "Failed to delete store.";
                    response.ResponsCode = 400;
                    return BadRequest(response);
                }

                // If it's a Minew store, try to delete from cloud
                if (store.StoreType == "minew" && !string.IsNullOrEmpty(store.MinewStoreId))
                {
                    try
                    {
                        var token = await GetToken();
                        await _minewService.OpenOrCloseStoreAsync(store.MinewStoreId, 0);
                    }
                    catch (Exception syncEx)
                    {
                        _logger.LogWarning(syncEx, "Failed to delete store from cloud");
                    }
                }

                response.Success = true;
                response.Message = "Store deleted successfully.";
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting store with ID: {Id}", id);
                response.Success = false;
                response.Message = "Failed to delete store.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Sync stores with Minew cloud
        /// </summary>
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost("sync")]
        [ProducesResponseType(typeof(HttpResponseData<StoreSyncResultDto>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> SyncStores([FromBody] StoreSyncRequestDto request)
        {
            var response = new HttpResponseData<StoreSyncResultDto>();
            try
            {
                var result = new StoreSyncResultDto();

                // Sync from cloud to local
                if (request.SyncFromCloud)
                {
                    await SyncFromCloud(result);
                }

                // Sync local stores to cloud
                if (request.SyncToCloud)
                {
                    await SyncToCloud(request.StoreIds, result);
                }

                response.Success = true;
                response.Message = "Store sync completed.";
                response.Result = result;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing stores");
                response.Success = false;
                response.Message = "Failed to sync stores.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets stores from Minew cloud
        /// </summary>
        [HttpGet("minew-cloud")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetMinewCloudStores()
        {
            var response = new HttpResponseData<object>();
            try
            {
                var token = await GetToken();
                var cloudStores = await _minewService.GetStoresAsync(token);

                response.Success = true;
                response.Message = "Cloud stores retrieved successfully.";
                response.Result = cloudStores;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cloud stores");
                response.Success = false;
                response.Message = "Failed to get cloud stores.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets store statistics
        /// </summary>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetStoreStatistics()
        {
            var response = new HttpResponseData<object>();
            try
            {
                // Get total stores count
                var totalStores = await _storeRepo.GetStoresAsync(new StoreFilterDto { PageSize = 1 });

                // Get active stores count
                var activeStores = await _storeRepo.GetStoresAsync(new StoreFilterDto
                {
                    PageSize = 1,
                    IsActive = true
                });

                // Get minew stores count
                var minewStores = await _storeRepo.GetStoresAsync(new StoreFilterDto
                {
                    PageSize = 1,
                    StoreType = "minew"
                });

                var statistics = new
                {
                    TotalStores = totalStores.TotalCount,
                    ActiveStores = activeStores.TotalCount,
                    InactiveStores = totalStores.TotalCount - activeStores.TotalCount,
                    MinewStores = minewStores.TotalCount,
                    LocalStores = totalStores.TotalCount - minewStores.TotalCount,
                    SyncedStores = 0, // You need to implement this
                    PendingSyncStores = 0 // You need to implement this
                };

                response.Success = true;
                response.Message = "Store statistics retrieved successfully.";
                response.Result = statistics;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting store statistics");
                response.Success = false;
                response.Message = "Failed to get store statistics.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // Helper methods
        //private async Task SyncFromCloud(StoreSyncResultDto result)
        //{
        //    try
        //    {
        //        var token = await GetToken();
        //        var cloudStores = await _minewService.GetStoresAsync(token);

        //        if (string.IsNullOrEmpty(token))
        //        {
        //            return Unauthorized("Failed to get Minew authentication token");
        //        }

        //        var syncCount = await _storeRepo.SyncMinewStoresAsync(token, 1, null);
        //        result.Details.Add(new StoreSyncDetailDto
        //        {
        //            StoreId = 0,
        //            StoreName = "Cloud Sync",
        //            Operation = "completed",
        //            Message = "Fetched stores from cloud",
        //            Timestamp = DateTime.UtcNow
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error syncing from cloud");
        //        result.Details.Add(new StoreSyncDetailDto
        //        {
        //            StoreId = 0,
        //            StoreName = "Cloud Sync",
        //            Operation = "failed",
        //            Message = ex.Message,
        //            Timestamp = DateTime.UtcNow
        //        });
        //        result.FailedCount++;
        //    }
        //}
        private async Task SyncFromCloud(StoreSyncResultDto result)
        {
            try
            {
                result.Details.Add(new StoreSyncDetailDto
                {
                    StoreId = 0,
                    StoreName = "Cloud Sync",
                    Operation = "started",
                    Message = "Starting cloud sync...",
                    Timestamp = DateTime.UtcNow
                });

                // Get token
                string token = await GetToken();

                if (string.IsNullOrEmpty(token))
                {
                    throw new UnauthorizedAccessException("Failed to get authentication token");
                }

                // Use the existing repository method - fixed variable name
                var syncCount = await _storeRepo.SyncMinewStoresAsync(token, 1, null);

                result.Details.Add(new StoreSyncDetailDto
                {
                    StoreId = 0,
                    StoreName = "Cloud Sync",
                    Operation = "completed",
                    Message = $"Cloud sync completed: {syncCount} stores synchronized",
                    Timestamp = DateTime.UtcNow
                });

                // Fixed: Changed result.SyncedFromCloud to match DTO property name
                result.SyncedCount = syncCount;
                result.TotalSynced += syncCount;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Authentication failed during cloud sync");
                result.Details.Add(new StoreSyncDetailDto
                {
                    StoreId = 0,
                    StoreName = "Cloud Sync",
                    Operation = "auth_failed",
                    Message = "Authentication failed: " + ex.Message,
                    Timestamp = DateTime.UtcNow
                });
                result.FailedCount++;
                throw; // Re-throw auth failures
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing from cloud");
                result.Details.Add(new StoreSyncDetailDto
                {
                    StoreId = 0,
                    StoreName = "Cloud Sync",
                    Operation = "failed",
                    Message = $"Cloud sync failed: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                });
                result.FailedCount++;
                // Don't throw here to allow local-to-cloud sync to proceed if possible
            }
        }

        private async Task SyncToCloud(List<long> storeIds, StoreSyncResultDto result)
        {
            var storesToSync = await _storeRepo.GetStoresForSyncAsync(storeIds);

            foreach (var store in storesToSync)
            {
                try
                {
                    await SyncStoreToCloud(store.Id);
                    result.SyncedCount++;

                    result.Details.Add(new StoreSyncDetailDto
                    {
                        StoreId = store.Id,
                        StoreName = store.StoreName,
                        Operation = "updated",
                        Message = "Synced to cloud successfully",
                        Timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    result.FailedCount++;

                    result.Details.Add(new StoreSyncDetailDto
                    {
                        StoreId = store.Id,
                        StoreName = store.StoreName,
                        Operation = "failed",
                        Message = ex.Message,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
        }

        /// <summary>
        /// Pushes a local store up to the Minew cloud and records the cloud id.
        /// Throws when the cloud rejects the call, having first marked the row
        /// as failed - a store must never be left flagged "success" while the
        /// cloud knows nothing about it.
        /// </summary>
        private async Task SyncStoreToCloud(long storeId)
        {
            var store = await _storeRepo.GetStoreByIdAsync(storeId);
            if (store == null || store.StoreType != "minew")
                return;

            var token = await GetToken();

            try
            {
                if (string.IsNullOrEmpty(store.MinewStoreId))
                {
                    // `number` is Minew's store code and is mandatory on add;
                    // leaving it empty is why these calls used to be rejected.
                    var addRequest = new MinewAddStoreRequest
                    {
                        number = !string.IsNullOrWhiteSpace(store.StoreCode)
                            ? store.StoreCode.Trim()
                            : $"ST{store.Id}",
                        name = store.StoreName,
                        address = store.Address ?? string.Empty,
                    };

                    // Minew rejects a blank number, name or address with a
                    // localised error (code 54029). Catch it here so the caller
                    // gets something actionable instead.
                    if (string.IsNullOrWhiteSpace(addRequest.address))
                    {
                        throw new Exception(
                            "Minew requires an address for a cloud store. " +
                            "Set the store's address and sync again.");
                    }
                    if (string.IsNullOrWhiteSpace(addRequest.name))
                    {
                        throw new Exception("Minew requires a store name for a cloud store.");
                    }

                    var response = await _minewService.AddStoreAsync(addRequest);
                    EnsureMinewSucceeded(response, "add store");

                    // Add may return the new id directly, or nothing useful - in
                    // which case find it by the number we just registered.
                    store.MinewStoreId =
                        ExtractMinewStoreId(response)
                        ?? await FindCloudStoreIdAsync(token, addRequest.number, store.StoreName);

                    if (string.IsNullOrEmpty(store.MinewStoreId))
                    {
                        throw new Exception(
                            "Minew accepted the store but no cloud id could be resolved; " +
                            "refusing to mark it synced.");
                    }
                }
                else
                {
                    var updateRequest = new MinewUpdateStoreRequest
                    {
                        id = store.MinewStoreId,
                        name = store.StoreName,
                        address = store.Address ?? string.Empty,
                        active = store.IsActive ? 1 : 0,
                    };

                    var response = await _minewService.UpdateStoreAsync(updateRequest);
                    EnsureMinewSucceeded(response, "update store");
                }

                store.IsSynced = true;
                store.SyncStatus = "success";
                store.LastSyncDate = DateTime.UtcNow;
                await _storeRepo.UpdateStoreAsync(store);
            }
            catch
            {
                store.IsSynced = false;
                store.SyncStatus = "failed";
                store.LastSyncDate = DateTime.UtcNow;
                await _storeRepo.UpdateStoreAsync(store);
                throw;
            }
        }

        /// <summary>
        /// Minew signals failure in the body (code != 200) rather than by HTTP
        /// status, so an unchecked response reads as success.
        /// </summary>
        private static void EnsureMinewSucceeded(string rawResponse, string operation)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
                throw new Exception($"Minew {operation} returned an empty response.");

            int? code = null;
            string message = null;

            try
            {
                using var doc = JsonDocument.Parse(rawResponse);
                if (doc.RootElement.TryGetProperty("code", out var codeEl))
                {
                    if (codeEl.ValueKind == JsonValueKind.Number && codeEl.TryGetInt32(out var c))
                        code = c;
                    else if (codeEl.ValueKind == JsonValueKind.String &&
                             int.TryParse(codeEl.GetString(), out var cs))
                        code = cs;
                }
                if (doc.RootElement.TryGetProperty("msg", out var msgEl))
                    message = msgEl.GetString();
            }
            catch (JsonException)
            {
                throw new Exception(
                    $"Minew {operation} returned an unreadable response: {Truncate(rawResponse)}");
            }

            if (code != 200)
            {
                throw new Exception(
                    $"Minew {operation} failed (code {code?.ToString() ?? "none"}): " +
                    $"{message ?? Truncate(rawResponse)}");
            }
        }

        /// <summary>
        /// Digs the created store's id out of an add response. The payload shape
        /// varies (bare id, object, or single-element array), so try each.
        /// </summary>
        private static string ExtractMinewStoreId(string rawResponse)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawResponse);
                if (!doc.RootElement.TryGetProperty("data", out var data))
                    return null;

                switch (data.ValueKind)
                {
                    case JsonValueKind.String:
                        return NullIfBlank(data.GetString());
                    case JsonValueKind.Number:
                        return data.GetRawText();
                    case JsonValueKind.Object:
                        return data.TryGetProperty("id", out var idEl)
                            ? NullIfBlank(idEl.ValueKind == JsonValueKind.String
                                ? idEl.GetString()
                                : idEl.GetRawText())
                            : null;
                    case JsonValueKind.Array:
                        foreach (var item in data.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.Object &&
                                item.TryGetProperty("id", out var arrId))
                            {
                                return NullIfBlank(arrId.ValueKind == JsonValueKind.String
                                    ? arrId.GetString()
                                    : arrId.GetRawText());
                            }
                        }
                        return null;
                    default:
                        return null;
                }
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Fallback when add does not echo the id: list the cloud's stores and
        /// match on the number we registered, then on name.
        /// </summary>
        private async Task<string> FindCloudStoreIdAsync(string token, string number, string name)
        {
            try
            {
                var listed = await _minewService.GetStoresAsync(token);
                var items = listed?.data;
                if (items == null || items.Count == 0)
                    return null;

                var match =
                    items.FirstOrDefault(s =>
                        !string.IsNullOrEmpty(number) &&
                        string.Equals(s.name, number, StringComparison.OrdinalIgnoreCase))
                    ?? items.FirstOrDefault(s =>
                        string.Equals(s.name, name, StringComparison.OrdinalIgnoreCase));

                return NullIfBlank(match?.id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not list Minew stores to resolve the new store id");
                return null;
            }
        }

        private static string NullIfBlank(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string Truncate(string value) =>
            value.Length <= 300 ? value : value.Substring(0, 300) + "...";

        private async Task<string> GetToken()
        {
            try
            {
                return await _minewService.GetValidTokenAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get authentication token");
                throw new Exception("Failed to get authentication token");
            }
        }
    }
}
