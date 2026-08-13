using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Data;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Interface;
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Models.shelf;
using TERMS_LOYALTY_API.Services;
using static TERMS_LOYALTY_API.DTOs.shelf.MinewStore;

namespace TERMS_LOYALTY_API.Repository
{
    public class StoreRepository : IStore
    {
        private readonly SmartShelfDbContext _context;
        private readonly MinewCloudService _minewService;
        private readonly ILogger<StoreRepository> _logger;
        public StoreRepository(SmartShelfDbContext context, MinewCloudService minewCloudService,ILogger<StoreRepository> logger)
        {
            _context = context;
            _minewService = minewCloudService;
            _logger = logger;
        }
        public async Task<StoreMaster> AddAsync(StoreMaster data)
        {
            data.CreatedDate = DateTime.UtcNow;
            await _context.StoreMaster.AddAsync(data);
            await _context.SaveChangesAsync();
            return data;
        }

        public async Task<PagedResult<StoreMaster>> GetStoresAsync(StoreFilterDto filter)
        {
            try
            {
                var query = _context.StoreMaster.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    query = query.Where(s =>
                        s.StoreName.Contains(filter.SearchTerm) ||
                        s.StoreCode.Contains(filter.SearchTerm) ||
                        s.Address.Contains(filter.SearchTerm) ||
                        s.ContactPerson.Contains(filter.SearchTerm) ||
                        s.Phone.Contains(filter.SearchTerm));
                }

                if (!string.IsNullOrEmpty(filter.StoreType))
                    query = query.Where(s => s.StoreType == filter.StoreType);

                if (filter.IsActive.HasValue)
                    query = query.Where(s => s.IsActive == filter.IsActive.Value);

                if (filter.IsSynced.HasValue)
                    query = query.Where(s => s.IsSynced == filter.IsSynced.Value);

                if (filter.CreatedFrom.HasValue)
                    query = query.Where(s => s.CreatedDate >= filter.CreatedFrom.Value);

                if (filter.CreatedTo.HasValue)
                    query = query.Where(s => s.CreatedDate <= filter.CreatedTo.Value);

                var totalCount = await query.CountAsync();

                // Sorting
                query = filter.SortBy?.ToLower() switch
                {
                    "storename" => filter.SortDirection == "asc"
                        ? query.OrderBy(s => s.StoreName)
                        : query.OrderByDescending(s => s.StoreName),

                    "storecode" => filter.SortDirection == "asc"
                        ? query.OrderBy(s => s.StoreCode)
                        : query.OrderByDescending(s => s.StoreCode),

                    "createddate" => filter.SortDirection == "asc"
                        ? query.OrderBy(s => s.CreatedDate)
                        : query.OrderByDescending(s => s.CreatedDate),

                    _ => query.OrderByDescending(s => s.CreatedDate)
                };

                int skip = (filter.PageNumber - 1) * filter.PageSize;
                int take = filter.PageNumber * filter.PageSize; // fetch extra for client-side skip

                // Fetch enough rows from SQL and apply skip in memory
                var allFetched = await query
                    .Take(take)
                    .ToListAsync();

                var items = allFetched
                    .Skip(skip)
                    .Take(filter.PageSize)
                    .ToList();

                return new PagedResult<StoreMaster>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
                };
            }
            catch
            {
                throw;
            }
        }


        //public async Task<PagedResult<StoreMaster>> GetStoresAsync(StoreFilterDto filter)
        //{
        //    try
        //    {
        //        var query = _context.StoreMaster.AsQueryable();

        //        if (!string.IsNullOrEmpty(filter.SearchTerm))
        //        {
        //            query = query.Where(s =>
        //                s.StoreName.Contains(filter.SearchTerm) ||
        //                s.StoreCode.Contains(filter.SearchTerm) ||
        //                s.Address.Contains(filter.SearchTerm) ||
        //                s.ContactPerson.Contains(filter.SearchTerm) ||
        //                s.Phone.Contains(filter.SearchTerm));
        //        }

        //        if (!string.IsNullOrEmpty(filter.StoreType))
        //            query = query.Where(s => s.StoreType == filter.StoreType);

        //        if (filter.IsActive.HasValue)
        //            query = query.Where(s => s.IsActive == filter.IsActive.Value);

        //        if (filter.IsSynced.HasValue)
        //            query = query.Where(s => s.IsSynced == filter.IsSynced.Value);

        //        if (filter.CreatedFrom.HasValue)
        //            query = query.Where(s => s.CreatedDate >= filter.CreatedFrom.Value);

        //        if (filter.CreatedTo.HasValue)
        //            query = query.Where(s => s.CreatedDate <= filter.CreatedTo.Value);

        //        var totalCount = await query.CountAsync();

        //        // Sorting (MUST come before paging)
        //        query = filter.SortBy.ToLower() switch
        //        {
        //            "storename" => filter.SortDirection == "asc"
        //                ? query.OrderBy(s => s.StoreName)
        //                : query.OrderByDescending(s => s.StoreName),

        //            "storecode" => filter.SortDirection == "asc"
        //                ? query.OrderBy(s => s.StoreCode)
        //                : query.OrderByDescending(s => s.StoreCode),

        //            "createddate" => filter.SortDirection == "asc"
        //                ? query.OrderBy(s => s.CreatedDate)
        //                : query.OrderByDescending(s => s.CreatedDate),

        //            _ => query.OrderByDescending(s => s.CreatedDate)
        //        };

        //        int skip = (filter.PageNumber - 1) * filter.PageSize;

        //        var items = await query
        //            .Select((s, index) => new { Store = s, RowNum = index + 1 })
        //            .Where(x => x.RowNum > skip && x.RowNum <= skip + filter.PageSize)
        //            .Select(x => x.Store)
        //            .ToListAsync();

        //        return new PagedResult<StoreMaster>
        //        {
        //            Items = items,
        //            TotalCount = totalCount,
        //            PageNumber = filter.PageNumber,
        //            PageSize = filter.PageSize,
        //            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
        //        };
        //    }
        //    catch
        //    {
        //        throw;
        //    }
        //}

        public async Task<PagedResult<object>> GetStoreDetailsAsync(StoreFilterDto filter)
        {
            try
            {
                int pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;
                int pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;

                int startRow = (pageNumber - 1) * pageSize + 1;
                int endRow = pageNumber * pageSize;

                // Build the predicate once so the page query and the total count
                // always agree. Previously neither applied the filter at all, so
                // deactivated stores (a delete is a soft delete - IsActive=false)
                // came back in every store dropdown regardless of isActive=true.
                var conditions = new List<string>();
                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@StartRow", startRow),
                    new SqlParameter("@EndRow", endRow)
                };

                if (filter.IsActive.HasValue)
                {
                    conditions.Add("IsActive = @IsActive");
                    parameters.Add(new SqlParameter("@IsActive", filter.IsActive.Value));
                }
                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    conditions.Add("(StoreName LIKE @SearchTerm OR StoreCode LIKE @SearchTerm OR Address LIKE @SearchTerm)");
                    parameters.Add(new SqlParameter("@SearchTerm", "%" + filter.SearchTerm.Trim() + "%"));
                }
                if (!string.IsNullOrWhiteSpace(filter.StoreType))
                {
                    conditions.Add("StoreType = @StoreType");
                    parameters.Add(new SqlParameter("@StoreType", filter.StoreType));
                }
                if (filter.IsSynced.HasValue)
                {
                    conditions.Add("IsSynced = @IsSynced");
                    parameters.Add(new SqlParameter("@IsSynced", filter.IsSynced.Value));
                }
                if (filter.CreatedFrom.HasValue)
                {
                    conditions.Add("CreatedDate >= @CreatedFrom");
                    parameters.Add(new SqlParameter("@CreatedFrom", filter.CreatedFrom.Value));
                }
                if (filter.CreatedTo.HasValue)
                {
                    conditions.Add("CreatedDate <= @CreatedTo");
                    parameters.Add(new SqlParameter("@CreatedTo", filter.CreatedTo.Value));
                }

                // Only fixed SQL fragments are concatenated; every value is bound.
                var whereClause = conditions.Count > 0
                    ? "WHERE " + string.Join(" AND ", conditions)
                    : string.Empty;

                // Total count has to honour the same filter or paging lies.
                var countQuery = _context.StoreMaster.AsQueryable();
                if (filter.IsActive.HasValue)
                    countQuery = countQuery.Where(s => s.IsActive == filter.IsActive.Value);
                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    var term = filter.SearchTerm.Trim();
                    countQuery = countQuery.Where(s =>
                        s.StoreName.Contains(term) ||
                        s.StoreCode.Contains(term) ||
                        s.Address.Contains(term));
                }
                if (!string.IsNullOrWhiteSpace(filter.StoreType))
                    countQuery = countQuery.Where(s => s.StoreType == filter.StoreType);
                if (filter.IsSynced.HasValue)
                    countQuery = countQuery.Where(s => s.IsSynced == filter.IsSynced.Value);
                if (filter.CreatedFrom.HasValue)
                    countQuery = countQuery.Where(s => s.CreatedDate >= filter.CreatedFrom.Value);
                if (filter.CreatedTo.HasValue)
                    countQuery = countQuery.Where(s => s.CreatedDate <= filter.CreatedTo.Value);

                var totalCount = await countQuery.CountAsync();

                // SQL Server 2008 paging using ROW_NUMBER
                var stores = await _context.StoreMaster
                    .FromSqlRaw($@"
                SELECT *
                FROM (
                    SELECT *,
                           ROW_NUMBER() OVER (ORDER BY CreatedDate DESC) AS RowNum
                    FROM StoreMaster
                    {whereClause}
                ) T
                WHERE RowNum BETWEEN @StartRow AND @EndRow",
                        parameters.ToArray()
                    )
                    .AsNoTracking()
                    .ToListAsync();

                // Get device counts
                var storeIds = stores.Select(s => s.Id.ToString()).ToList();

                var deviceCounts = await _context.DeviceMaster
                    .Where(d => storeIds.Contains(d.StoreId.ToString()) && d.IsActive)
                    .GroupBy(d => d.StoreId)
                    .Select(g => new
                    {
                        StoreId = g.Key,
                        DeviceCount = g.Count()
                    })
                    .ToListAsync();

                var deviceCountDict = deviceCounts
                    .Where(x => long.TryParse(x.StoreId.ToString(), out _))
                    .ToDictionary(x => long.Parse(x.StoreId.ToString()), x => x.DeviceCount);

                var items = stores.Select(s => new
                {
                    s.Id,
                    s.StoreName,
                    s.StoreCode,
                    s.Address,
                    s.Phone,
                    s.Email,
                    s.ContactPerson,
                    s.StoreType,
                    s.MinewStoreId,
                    s.IsActive,
                    s.IsSynced,
                    s.CreatedDate,
                    DeviceCount = deviceCountDict.TryGetValue(s.Id, out var c) ? c : 0
                })
                .Cast<object>()
                .ToList();

                return new PagedResult<object>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
            }
            catch (Exception ex)
            {
                // Log exception here (ILogger recommended)

                throw new ApplicationException(
                    "Error occurred while fetching store details.", ex);
            }
        }


        public async Task<StoreMaster> GetStoreByIdAsync(long id)
        {
            return await _context.StoreMaster
                .Include(s => s.Devices)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<StoreMaster> CreateStoreAsync(StoreMaster store)
        {
                _context.StoreMaster.Add(store);
                await _context.SaveChangesAsync();
                return store;
        }

        public async Task<StoreMaster> UpdateStoreAsync(StoreMaster store)
        {
            _context.StoreMaster.Update(store);
            await _context.SaveChangesAsync();
            return store;
        }

        public async Task<bool> DeleteStoreAsync(long id)
        {
            var store = await _context.StoreMaster.FindAsync(id);
            if (store == null)
                return false;

            store.IsActive = false;
            store.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> StoreCodeExistsAsync(string storeCode, long? excludeId = null)
        {
            var query = _context.StoreMaster
                .Where(s => s.StoreCode == storeCode && s.IsActive);

            if (excludeId.HasValue)
            {
                query = query.Where(s => s.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<List<StoreMaster>> GetStoresForSyncAsync(List<long> storeIds = null)
        {
            var query = _context.StoreMaster
                .Where(s => s.StoreType == "minew" && s.IsActive);

            if (storeIds != null && storeIds.Any())
            {
                query = query.Where(s => storeIds.Contains(s.Id));
            }
            else
            {
                query = query.Where(s => !s.IsSynced);
            }

            return await query.ToListAsync();
        }

        public async Task<StoreMaster> GetStoreByMinewID(string Id)
        {
            return await _context.StoreMaster.Where(x => x.MinewStoreId == Id).FirstOrDefaultAsync();
        }
        public async Task<StoreMaster?> GetStoreByProductIdAsync(long productId)
        {
            return await _context.ProductMaster
                .Where(p => p.Id == productId)
                .Select(p => p.Store)
                .FirstOrDefaultAsync();
        }

        //public async Task<int> SyncMinewStoresAsync(string token, int active = 1, string condition = null)
        //{
        //    int createdCount = 0;
        //    int updatedCount = 0;

        //    try
        //    {
        //        // 1. Get stores from Minew API
        //        var minewResponse = await _minewService.GetStoresAsync(token, active, condition);

        //        if (minewResponse == null || minewResponse.code != 200 || minewResponse.data == null)
        //        {
        //            throw new Exception($"Failed to fetch stores from Minew API: {minewResponse?.msg ?? "Unknown error"}");
        //        }

        //        // 2. Get existing Minew stores from local database
        //        var existingMinewStores = await _context.StoreMaster
        //            .Where(s => s.StoreType == "minew" && !string.IsNullOrEmpty(s.MinewStoreId))
        //            .ToDictionaryAsync(s => s.MinewStoreId, s => s);

        //        // 3. Process each store from Minew API
        //        foreach (var minewStore in minewResponse.data)
        //        {
        //            // Check if store already exists locally
        //            if (existingMinewStores.TryGetValue(minewStore.id, out var existingStore))
        //            {
        //                // Update existing store
        //                var wasUpdated = UpdateStoreFromMinewData(existingStore, minewStore);
        //                if (wasUpdated)
        //                {
        //                    existingStore.LastSyncDate = DateTime.UtcNow;
        //                    existingStore.SyncStatus = "success";
        //                    existingStore.IsSynced = true;
        //                    updatedCount++;
        //                }
        //            }
        //            else
        //            {
        //                // Create new store
        //                var newStore = CreateStoreFromMinewData(minewStore);
        //                await _context.StoreMaster.AddAsync(newStore);
        //                createdCount++;
        //            }
        //        }

        //        // 4. Handle stores that might have been deleted from Minew
        //        var minewStoreIds = minewResponse.data.Select(s => s.id).ToList();
        //        var deletedStores = existingMinewStores
        //            .Where(kvp => !minewStoreIds.Contains(kvp.Key))
        //            .Select(kvp => kvp.Value)
        //            .ToList();

        //        foreach (var deletedStore in deletedStores)
        //        {
        //            deletedStore.IsActive = false;
        //            deletedStore.SyncStatus = "deleted_in_minew";
        //            deletedStore.UpdatedDate = DateTime.UtcNow;
        //        }

        //        // 5. Save all changes
        //        await _context.SaveChangesAsync();

        //        return createdCount + updatedCount;
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log the error
        //        throw new Exception($"Failed to sync Minew stores: {ex.Message}", ex);
        //    }
        //}
        public async Task<int> SyncMinewStoresAsync(string token, int active = 1, string? condition = null)
        {
            int createdCount = 0;
            int updatedCount = 0;

            try
            {
                // 1. Get stores from Minew API
                var minewResponse = await _minewService.GetStoresAsync(token, active, condition);

                if (minewResponse == null || minewResponse.code != 200 || minewResponse.data == null)
                {
                    throw new Exception($"Failed to fetch stores from Minew API: {minewResponse?.msg ?? "Unknown error"}");
                }

                // 2. Get existing Minew stores from local database - track them
                var existingMinewStores = await _context.StoreMaster
                    .Where(s => s.StoreType == "minew" && !string.IsNullOrEmpty(s.MinewStoreId))
                    .ToListAsync(); // Changed to List instead of Dictionary

                var existingMinewStoresDict = existingMinewStores
                    .Where(s => !string.IsNullOrEmpty(s.MinewStoreId))
                    .ToDictionary(s => s.MinewStoreId, s => s);

                // 3. Process each store from Minew API
                foreach (var minewStore in minewResponse.data)
                {
                    // Check if store already exists locally
                    if (existingMinewStoresDict.TryGetValue(minewStore.id, out var existingStore))
                    {
                        // Update existing store
                        var wasUpdated = UpdateStoreFromMinewData(existingStore, minewStore);
                        if (wasUpdated)
                        {
                            existingStore.LastSyncDate = DateTime.UtcNow;
                            existingStore.SyncStatus = "success";
                            existingStore.IsSynced = true;
                            existingStore.UpdatedDate = DateTime.UtcNow;
                            updatedCount++;
                        }
                    }
                    else
                    {
                        // Create new store
                        var newStore = CreateStoreFromMinewData(minewStore);
                        await _context.StoreMaster.AddAsync(newStore);
                        createdCount++;
                    }
                }

                // 4. Handle stores that might have been deleted from Minew
                var minewStoreIds = minewResponse.data.Select(s => s.id).ToList();
                var deletedStores = existingMinewStores
                    .Where(s => !string.IsNullOrEmpty(s.MinewStoreId) &&
                               !minewStoreIds.Contains(s.MinewStoreId))
                    .ToList();

                foreach (var deletedStore in deletedStores)
                {
                    deletedStore.IsActive = false;
                    deletedStore.SyncStatus = "deleted_in_minew";
                    deletedStore.UpdatedDate = DateTime.UtcNow;
                    deletedStore.LastSyncDate = DateTime.UtcNow;
                }

                // 5. Save all changes
                await _context.SaveChangesAsync();

                return createdCount + updatedCount;
            }
            catch (Exception ex)
            {
                // Log the error properly
                _logger.LogError(ex, "Failed to sync Minew stores");
                throw new Exception($"Failed to sync Minew stores: {ex.Message}", ex);
            }
        }

        //public async Task<int> SyncMinewStoresAsync(string token, int active = 1, string? condition = null)
        //{
        //    int createdCount = 0;
        //    int updatedCount = 0;

        //    try
        //    {
        //        // 1. Get stores from Minew API
        //        var minewResponse = await _minewService.GetStoresAsync(token, active, condition);

        //        if (minewResponse == null || minewResponse.code != 200 || minewResponse.data == null)
        //        {
        //            throw new Exception($"Failed to fetch stores from Minew API: {minewResponse?.msg ?? "Unknown error"}");
        //        }

        //        // 2. Get existing Minew stores from local database
        //        var existingMinewStores = await _context.StoreMaster
        //            .Where(s => s.StoreType == "minew" && !string.IsNullOrEmpty(s.MinewStoreId))
        //            .ToDictionaryAsync(s => s.MinewStoreId, s => s);

        //        // 3. Process each store from Minew API
        //        foreach (var minewStore in minewResponse.data)
        //        {
        //            // Check if store already exists locally
        //            if (existingMinewStores.TryGetValue(minewStore.id, out var existingStore))
        //            {
        //                // Update existing store
        //                var wasUpdated = UpdateStoreFromMinewData(existingStore, minewStore);
        //                if (wasUpdated)
        //                {
        //                    existingStore.LastSyncDate = DateTime.UtcNow;
        //                    existingStore.SyncStatus = "success";
        //                    existingStore.IsSynced = true;
        //                    existingStore.UpdatedDate = DateTime.UtcNow;
        //                    updatedCount++;
        //                }
        //            }
        //            else
        //            {
        //                // Create new store
        //                var newStore = CreateStoreFromMinewData(minewStore);
        //                await _context.StoreMaster.AddAsync(newStore);
        //                createdCount++;
        //            }
        //        }

        //        // 4. Handle stores that might have been deleted from Minew
        //        var minewStoreIds = minewResponse.data.Select(s => s.id).ToList();
        //        var deletedStores = existingMinewStores
        //            .Where(kvp => !minewStoreIds.Contains(kvp.Key))
        //            .Select(kvp => kvp.Value)
        //            .ToList();

        //        foreach (var deletedStore in deletedStores)
        //        {
        //            deletedStore.IsActive = false;
        //            deletedStore.SyncStatus = "deleted_in_minew";
        //            deletedStore.UpdatedDate = DateTime.UtcNow;
        //            deletedStore.LastSyncDate = DateTime.UtcNow;
        //        }

        //        // 5. Save all changes
        //        await _context.SaveChangesAsync();

        //        return createdCount + updatedCount;
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log the error properly
        //        _logger.LogError(ex, "Failed to sync Minew stores");
        //        throw new Exception($"Failed to sync Minew stores: {ex.Message}", ex);
        //    }
        //}

        private StoreMaster CreateStoreFromMinewData(MinewStoreItem minewStoreData)
        {
            return new StoreMaster
            {
                Id = 0, // Explicitly set to 0 for new entities
                StoreName = minewStoreData.name ?? $"Store-{minewStoreData.id}",
                StoreCode = minewStoreData.merchantCode ?? $"MINEW-{minewStoreData.id}",
                Address = minewStoreData.address,
                Phone = "", // Minew API doesn't provide phone
                Email = "", // Minew API doesn't provide email
                ContactPerson = "", // Minew API doesn't provide contact person
                StoreType = "minew",
                MinewStoreId = minewStoreData.id,
                Latitude = !string.IsNullOrEmpty(minewStoreData.latitude) &&
                          decimal.TryParse(minewStoreData.latitude, out var lat) ? lat : null,
                Longitude = !string.IsNullOrEmpty(minewStoreData.longitude) &&
                           decimal.TryParse(minewStoreData.longitude, out var lon) ? lon : null,
                IsActive = minewStoreData.active == 1,
                IsSynced = true,
                LastSyncDate = DateTime.UtcNow,
                SyncStatus = "success",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
        }

        private bool UpdateStoreFromMinewData(StoreMaster existingStore, MinewStoreItem minewStoreData)
        {
            bool wasUpdated = false;

            // Update store name if changed
            if (existingStore.StoreName != minewStoreData.name && !string.IsNullOrEmpty(minewStoreData.name))
            {
                existingStore.StoreName = minewStoreData.name;
                wasUpdated = true;
            }

            // Update address if changed
            if (existingStore.Address != minewStoreData.address && !string.IsNullOrEmpty(minewStoreData.address))
            {
                existingStore.Address = minewStoreData.address;
                wasUpdated = true;
            }

            // Update merchant code if changed
            if (existingStore.StoreCode != minewStoreData.merchantCode && !string.IsNullOrEmpty(minewStoreData.merchantCode))
            {
                existingStore.StoreCode = minewStoreData.merchantCode;
                wasUpdated = true;
            }

            // Update latitude if changed
            if (!string.IsNullOrEmpty(minewStoreData.latitude) &&
                decimal.TryParse(minewStoreData.latitude, out var lat) &&
                existingStore.Latitude != lat)
            {
                existingStore.Latitude = lat;
                wasUpdated = true;
            }

            // Update longitude if changed
            if (!string.IsNullOrEmpty(minewStoreData.longitude) &&
                decimal.TryParse(minewStoreData.longitude, out var lon) &&
                existingStore.Longitude != lon)
            {
                existingStore.Longitude = lon;
                wasUpdated = true;
            }

            // Update active status if changed
            var isActive = minewStoreData.active == 1;
            if (existingStore.IsActive != isActive)
            {
                existingStore.IsActive = isActive;
                wasUpdated = true;
            }

            // Always update the updated date if anything changed
            if (wasUpdated)
            {
                existingStore.UpdatedDate = DateTime.UtcNow;
            }

            return wasUpdated;
        }
    }
}
