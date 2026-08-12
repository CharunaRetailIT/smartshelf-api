using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Data;
using TERMS_LOYALTY_API.DTOs;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Interface;
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Models.shelf;
using TERMS_LOYALTY_API.Models.user;
using TERMS_MOBILE_WEB_API.Models;
using static TERMS_LOYALTY_API.DTOs.shelf.ProductSummary;

namespace TERMS_LOYALTY_API.Repository
{
    public class AisleRepository : IAisle
    {
        private readonly SmartShelfDbContext _context;

        public AisleRepository(SmartShelfDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AisleMaster>> GetAllAsync(long? storeId)
        {
            try
            {
                var query = _context.AisleMaster.AsQueryable();

                if (storeId.HasValue)
                {
                    query = query.Where(x => x.StoreId == storeId.Value); 
                }

                var result = await query
                    .OrderByDescending(a => a.CreatedDate)
                    //.Include(s => s.ShelfProducts)
                    //.ThenInclude(sp => sp.Product)
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                // Use proper logging instead of Console.WriteLine in production
                Console.WriteLine($"Error fetching aisles: {ex.Message}");
                throw;
            }
        }


        public async Task<AisleMaster> GetByIdAsync(long id)
        {
            return await _context.AisleMaster
                //.Include(s => s.ShelfProducts)
                //.ThenInclude(sp => sp.Product)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        //public async Task<AisleMaster> CreateAisleAsync(CreateAisleRequest request)
        //{
        //    var aisle = new AisleMaster
        //    {
        //        Name = request.Name,
        //        Description = request.Description,
        //        Location = request.Location,
        //        Coordinates = request.Coordinates,
        //        StoreId = request.StoreId,
        //        IsActive = request.IsActive,
        //        CreatedDate = DateTime.UtcNow,
        //        CreatedUser = request.createdUser,
        //        Shelves = new List<ShelfMaster>()
        //    };

        //    if (request.Shelves != null && request.Shelves.Any())
        //    {
        //        foreach (var shelf in request.Shelves)
        //        {
        //            aisle.Shelves.Add(new ShelfMaster
        //            {
        //                Name = shelf.Name,
        //                Location = shelf.Location,
        //                Coordinates = shelf.Coordinates,
        //                Description = shelf.Description,
        //                StoreId = request.StoreId,
        //                IsActive = shelf.IsActive,
        //                CreatedDate = DateTime.UtcNow,
        //                CreatedUser = request.createdUser
        //            });
        //        }
        //    }

        //    _context.AisleMaster.Add(aisle);
        //    await _context.SaveChangesAsync();

        //    return aisle;
        //}

        public async Task<AisleMaster> CreateAisleAsync(CreateAisleRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var aisle = new AisleMaster
                {
                    Name = request.Name,
                    Description = request.Description,
                    Location = request.Location,
                    Coordinates = request.Coordinates,
                    StoreId = request.StoreId,
                    IsActive = request.IsActive,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = request.createdUser,
                    Shelves = new List<ShelfMaster>()
                };

                if (request.Shelves != null && request.Shelves.Any())
                {
                    foreach (var shelfRequest in request.Shelves)
                    {
                        var shelf = new ShelfMaster
                        {
                            Name = shelfRequest.Name,
                            Location = shelfRequest.Location,
                            Coordinates = shelfRequest.Coordinates,
                            Description = shelfRequest.Description,
                            StoreId = request.StoreId,
                            IsActive = shelfRequest.IsActive,
                            CreatedDate = DateTime.UtcNow,
                            CreatedUser = request.createdUser,
                            Aisle = aisle
                        };

                        aisle.Shelves.Add(shelf);
                    }
                }

                // Save aisle with shelves first to get IDs
                _context.AisleMaster.Add(aisle);
                await _context.SaveChangesAsync(); // This generates IDs for aisle and shelves

                var shelfRequests = request.Shelves.ToList();
                var shelves = aisle.Shelves.ToList();

                // Now process device assignments with valid shelf IDs
                if (request.Shelves != null && request.Shelves.Any())
                {
                    for (int i = 0; i < request.Shelves.Count; i++)
                    {
                        var shelfRequest = request.Shelves[i];
                        //  var shelf = aisle.Shelves[i]; // This now has a valid ID
                        //var lastaisle = await _context.AisleMaster.OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync();
                        // var shelf = await _context.ShelfMaster.FirstOrDefaultAsync(x => x.AisleId == lastaisle.Id);

                        var shelf = shelves[i];

                        if (shelfRequest.DeviceAssignments != null && shelfRequest.DeviceAssignments.Any())
                        {
                            await ProcessShelfDeviceAssignments(
                                shelfRequest,
                                shelf,
                                request.StoreId,
                                request.createdUser);
                        }
                    }
                }

                await _context.SaveChangesAsync(); // Save the assignments
                await transaction.CommitAsync();

                return aisle;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task ProcessShelfDeviceAssignments(CreateShelfRequest shelfRequest, ShelfMaster shelf,long storeId,int userId)
        {
            foreach (var assignmentRequest in shelfRequest.DeviceAssignments)
            {
                if (string.IsNullOrWhiteSpace(assignmentRequest.AssignmentType))
                    continue;

                long? comboId = null;
                string assignmentType = assignmentRequest.AssignmentType.ToUpper();

                // Handle combo creation/lookup
                if (assignmentType == "TEMPLATE")
                {
                    comboId = await HandleTemplateCombo(assignmentRequest, userId);
                }
                else if (assignmentType == "MESSAGE")
                {
                    comboId = await HandleMessageCombo(assignmentRequest, storeId, userId);
                }

                if (!comboId.HasValue)
                {
                    //_logger.LogWarning($"Failed to create/find combo for assignment on shelf {shelf.Id}");
                    continue;
                }

                // Create the assignment
                var assignment = new DeviceAssignment
                {
                    AssignmentType = assignmentType,
                    DeviceTemplateComboId = assignmentType == "TEMPLATE" ? comboId : null,
                    DeviceMessageComboId = assignmentType == "MESSAGE" ? comboId : null,
                    LocationType = "SHELF",
                    LocationId = shelf.Id,
                    DisplayOrder = assignmentRequest.DisplayOrder,
                    StoreId = storeId,
                    IsActive = assignmentRequest.IsActive,
                    CreatedUser = userId,
                    UpdatedUser = userId,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                _context.DeviceAssignment.Add(assignment);
            }
        }

        private async Task<long?> HandleTemplateCombo(DeviceAssignmentRequest request, int userId)
        {
            // Use existing combo if provided
            if (request.DeviceTemplateComboId.HasValue)
            {
                var existingCombo = await _context.DeviceTemplateCombos
                    .FirstOrDefaultAsync(c => c.Id == request.DeviceTemplateComboId.Value && c.IsActive);

                if (existingCombo != null)
                    return existingCombo.Id;
            }

            // Create new combo if device and template are provided
            if (request.DeviceId.HasValue && !string.IsNullOrEmpty(request.TemplateId))
            {
                return await CreateOrGetDeviceTemplateCombo(
                    request.DeviceId.Value,
                    request.TemplateId,
                    request.IsDefault,
                    userId
                );
            }

            return null;
        }

        private async Task<long?> HandleMessageCombo(DeviceAssignmentRequest request, long storeId, int userId)
        {
            // Use existing combo if provided
            if (request.DeviceMessageComboId.HasValue)
            {
                var existingCombo = await _context.DeviceMessageCombos
                    .FirstOrDefaultAsync(c => c.Id == request.DeviceMessageComboId.Value && c.IsActive);

                if (existingCombo != null)
                    return existingCombo.Id;
            }

            // Create new combo if device and message are provided
            if (request.DeviceId.HasValue && request.MessageId.HasValue)
            {
                return await CreateOrGetDeviceMessageCombo(
                    request.DeviceId.Value,
                    request.MessageId.Value,
                    storeId,
                    userId
                );
            }

            return null;
        }

        private async Task<long> CreateOrGetDeviceTemplateCombo(long deviceId, string templateId, bool isDefault, int userId)
        {
            // Check if combo already exists
            var existingCombo = await _context.DeviceTemplateCombos
                .FirstOrDefaultAsync(c => c.DeviceId == deviceId && c.TemplateId == templateId && c.IsActive);

            if (existingCombo != null)
            {
                // Update if default status changed
                if (existingCombo.IsDefault != isDefault)
                {
                    existingCombo.IsDefault = isDefault;
                    existingCombo.Priority = isDefault ? 1 : 0;
                    existingCombo.UpdatedDate = DateTime.UtcNow;
                    existingCombo.UpdatedUser = userId;
                    await _context.SaveChangesAsync();
                }
                return existingCombo.Id;
            }

            // Create new combo
            var combo = new DeviceTemplateCombos
            {
                DeviceId = deviceId,
                TemplateId = templateId,
                IsDefault = isDefault,
                Priority = isDefault ? 1 : 0,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                CreatedUser = userId,
                UpdatedUser = userId
            };

            _context.DeviceTemplateCombos.Add(combo);
            await _context.SaveChangesAsync();

            return combo.Id;
        }

        private async Task<long> CreateOrGetDeviceMessageCombo(long deviceId, long messageId, long storeId, int userId)
        {
            // Check if combo already exists
            var existingCombo = await _context.DeviceMessageCombos
                .FirstOrDefaultAsync(c => c.DeviceId == deviceId && c.MessageId == messageId && c.IsActive);

            if (existingCombo != null)
                return existingCombo.Id;

            // Create new combo
            var combo = new DeviceMessageCombos
            {
                DeviceId = deviceId,
                MessageId = messageId,
                StoreId = storeId,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                CreatedUser = userId,
                UpdatedUser = userId
            };

            _context.DeviceMessageCombos.Add(combo);
            await _context.SaveChangesAsync();

            return combo.Id;
        }

        public async Task<AisleMaster> AddAsync(AisleMaster data)
        {
            try
            {
                _context.AisleMaster.Add(data);
                await _context.SaveChangesAsync();
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

        }

        public async Task<AisleMaster> UpdateAsync(AisleMaster data)
        {
            var existing = await _context.AisleMaster.FindAsync(data.Id);
            if (existing == null) return null;

            // Apply only the editable fields rather than a blind full-entity
            // Update() - the client payload doesn't include StoreId (NOT NULL
            // in the DB) or CreatedDate/CreatedUser, so replacing the whole
            // row with the deserialized object would null/zero those out and
            // violate the StoreId NOT NULL constraint.
            existing.Name = data.Name;
            existing.Description = data.Description;
            existing.Location = data.Location;
            existing.Coordinates = data.Coordinates;
            existing.IsActive = data.IsActive;
            existing.UpdatedDate = DateTime.UtcNow;
            existing.UpdatedUser = data.UpdatedUser;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var data = await _context.AisleMaster.FindAsync(id);
            if (data == null) return false;

            _context.AisleMaster.Remove(data);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task AssignProductAsync(long aisleId, long productId)
        {
            if (!_context.ProductAssignments.Any(x => x.AisleId == aisleId && x.ProductId == productId))
            {
                _context.ProductAssignments.Add(new ProductAssignment
                {
                    AisleId = aisleId,
                    ProductId = productId
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveProductAsync(long asileId, long productId, int userId)
        {
            try
            {
                var product = await _context.ProductAssignments
                    .FirstOrDefaultAsync(sp => sp.AisleId == asileId && sp.ProductId == productId);

                if (product != null)
                {
                    product.UpdatedDate = DateTime.UtcNow;
                    product.UpdatedUser = userId;
                    product.IsActive = false;
                    _context.ProductAssignments.Update(product);
                    await _context.SaveChangesAsync();
                }
            }
            catch (DbUpdateException dbEx)
            {
                // Log database-specific errors
                throw new Exception("Database error occurred while removing product from shelf", dbEx);
            }
            catch (Exception ex)
            {
                // Log general errors
                throw new Exception("An error occurred while removing product from shelf", ex);
            }
        }

        public async Task<IEnumerable<ProductViewDto>> GetProductsByAisleAsync(long aisleId)
        {
            var query = from sp in _context.ProductAssignments
                        join pm in _context.ProductMaster on sp.ProductId equals pm.Id
                        join c in _context.ProductCategories on pm.CategoryId equals c.Id
                        join d in _context.ProductSubCategories on pm.SubCategoryId equals d.Id
                        where sp.AisleId == aisleId && sp.IsActive
                        select new ProductViewDto
                        {
                            Id = pm.Id,
                            ProductCode = pm.ProductCode,
                            CategoryName = c.CategoryName,
                            Description = pm.Description,
                            SubCategoryName = d.SubCategoryName,
                            SellingPrice = pm.SellingPrice,
                            DiscountPrice = pm.DiscountPrice,
                            ProductName = pm.ProductName,
                            BarCode = pm.BarCode,
                        };

            return await query.ToListAsync();
        }

        public async Task DeleteAisleAsync(long id, int user)
        {
            try
            {
                var data = await _context.AisleMaster
                    .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

                if (data != null)
                {
                    data.UpdatedDate = DateTime.UtcNow;
                    data.UpdatedUser = user;
                    data.IsActive = false;
                    _context.AisleMaster.Update(data);
                    await _context.SaveChangesAsync();
                }
            }
            catch (DbUpdateException dbEx)
            {
                // Log database-specific errors
                throw new Exception("Database error occurred while deleting shelf", dbEx);
            }
            catch (Exception ex)
            {
                // Log general errors
                throw new Exception("An error occurred while deleting shelf", ex);
            }
        }
        public async Task RestoreAisleAsync(long id, int user)
        {
            try
            {
                var data = await _context.AisleMaster
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsActive);

                if (data != null)
                {
                    data.UpdatedDate = DateTime.UtcNow;
                    data.UpdatedUser = user;
                    data.IsActive = true;
                    _context.AisleMaster.Update(data);
                    await _context.SaveChangesAsync();
                }
            }
            catch (DbUpdateException dbEx)
            {
                throw new Exception("Database error occurred while restoring aisle", dbEx);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while restoring aisle", ex);
            }
        }

        public async Task<IEnumerable<ShelfMaster>> GetShelvesByAilse(long id)
        {
            try
            {
                var shelves = _context.ShelfMaster.Where(x => x.AisleId == id);
                return await shelves.ToListAsync();
            }
            catch (Exception ex)
            {

                throw new Exception("An error occurred while loading shelve", ex);
            }

        }

        //public async Task<IEnumerable<AisleMaster>> GetAilsewithShelves()
        //{
        //    try
        //    {
        //        var data = await _context.AisleMaster.Include(x => x.Shelves).OrderByDescending(a => a.Id).ToListAsync();
        //        return data;
        //    }
        //    catch (Exception ex)
        //    {

        //        throw new Exception("An error occurred while loading shelve", ex);
        //    }
        //}

        //public async Task<List<AisleMasterWithShelvesDto>> GetAisleFullDetails()
        //{
        //    try
        //    {
        //        // Single query with projection - most efficient
        //        var result = await _context.AisleMaster
        //            .OrderByDescending(a => a.Id)
        //            .Select(aisle => new AisleMasterWithShelvesDto
        //            {
        //                Id = aisle.Id,
        //                Name = aisle.Name,
        //                Description = aisle.Description,
        //                IPAddress = aisle.IPAddress,
        //                NetworkName = aisle.NetworkName,
        //                MACAddress = aisle.MACAddress,
        //                Location = aisle.Location,
        //                Coordinates = aisle.Coordinates,
        //                DeviceName = aisle.DeviceName,
        //                IsActive = aisle.IsActive,
        //                Shelves = aisle.Shelves.Select(shelf => new ShelfMasterWithAssignmentsDto
        //                {
        //                    Id = shelf.Id,
        //                    AisleId = shelf.AisleId,
        //                    Name = shelf.Name,
        //                    Location = shelf.Location,
        //                    Coordinates = shelf.Coordinates,
        //                    IPAddress = shelf.IPAddress,
        //                    DeviceName = shelf.DeviceName,
        //                    MACAddress = shelf.MACAddress,
        //                    Description = shelf.Description,
        //                    IsActive = shelf.IsActive,
        //                    // Subquery for assignments
        //                    Assignments = _context.DeviceTemplateAssignment
        //                        .Where(dta => dta.LocationType == "Shelf" &&
        //                                      dta.LocationId == shelf.Id &&
        //                                      dta.IsActive)
        //                        .Select(dta => new ShelfAssignmentDto
        //                        {
        //                            DeviceTemplateComboId = dta.DeviceTemplateComboId,
        //                            DeviceMAC = dta.DeviceTemplateCombo.Device.MACAddress,
        //                            TemplateName = dta.DeviceTemplateCombo.Template.Name,
        //                            DisplayOrder = dta.DisplayOrder
        //                        })
        //                        .ToList()
        //                }).ToList()
        //            })
        //            .ToListAsync();

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("An error occurred while loading aisles with shelves and assignments", ex);
        //    }
        //}

        //public async Task<PagedResult<AisleMasterWithShelvesDto>> GetAisleFullDetails(int page = 1, int pageSize = 10, string? search = null, string? status = null)
        //{
        //    try
        //    {
        //        var query = _context.AisleMaster.AsQueryable();

        //        // Apply status filter
        //        if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
        //        {
        //            bool isActive = status.ToLower() == "active";
        //            query = query.Where(a => a.IsActive == isActive);
        //        }

        //        // Apply search filter
        //        if (!string.IsNullOrEmpty(search))
        //        {
        //            search = search.ToLower();
        //            query = query.Where(a =>
        //                a.Name.ToLower().Contains(search) ||
        //                a.Location.ToLower().Contains(search) ||
        //                a.Description.ToLower().Contains(search));
        //        }

        //        // Get total count
        //        var totalCount = await query.CountAsync();

        //        // Apply ordering first
        //        var orderedQuery = query.OrderByDescending(a => a.Id);

        //        // Get all IDs for the page (compatible with SQL Server 2008)
        //        var aisleIds = await orderedQuery
        //            .Select(a => a.Id)
        //            .Skip((page - 1) * pageSize)
        //            .Take(pageSize)
        //            .ToListAsync();

        //        // Now fetch the full data for these IDs
        //        var items = await _context.AisleMaster
        //            .Where(a => aisleIds.Contains(a.Id))
        //            .OrderByDescending(a => a.Id) // Re-apply ordering to maintain order
        //            .Select(aisle => new AisleMasterWithShelvesDto
        //            {
        //                Id = aisle.Id,
        //                Name = aisle.Name,
        //                Description = aisle.Description,
        //                Location = aisle.Location,
        //                Coordinates = aisle.Coordinates,
        //                DeviceName = aisle.DeviceName,
        //                IsActive = aisle.IsActive,
        //                CreatedUser = aisle.CreatedUser,
        //                CreatedDate = aisle.CreatedDate,
        //                UpdatedUser = aisle.UpdatedUser,
        //                UpdatedDate = aisle.UpdatedDate,
        //                Shelves = aisle.Shelves.Select(shelf => new ShelfMasterWithAssignmentsDto
        //                {
        //                    Id = shelf.Id,
        //                    AisleId = shelf.AisleId,
        //                    Name = shelf.Name,
        //                    Location = shelf.Location,
        //                    Coordinates = shelf.Coordinates,
        //                    IPAddress = shelf.IPAddress,
        //                    DeviceName = shelf.DeviceName,
        //                    MACAddress = shelf.MACAddress,
        //                    Description = shelf.Description,
        //                    IsActive = shelf.IsActive,
        //                    CreatedUser = shelf.CreatedUser,
        //                    CreatedDate = shelf.CreatedDate,
        //                    UpdatedUser = shelf.UpdatedUser,
        //                    UpdatedDate = shelf.UpdatedDate,
        //                    Assignments = _context.DeviceTemplateAssignment
        //                        .Where(dta => dta.LocationType == "Shelf" &&
        //                                     dta.LocationId == shelf.Id &&
        //                                     dta.IsActive)
        //                        .Select(dta => new ShelfAssignmentDto
        //                        {
        //                            DeviceTemplateComboId = dta.DeviceTemplateComboId,
        //                            DeviceMAC = dta.DeviceTemplateCombo.Device.MACAddress,
        //                            TemplateName = dta.DeviceTemplateCombo.Template.Name,
        //                            DisplayOrder = dta.DisplayOrder,
        //                            IsActive = dta.IsActive
        //                        })
        //                        .ToList()
        //                }).ToList()
        //            })
        //            .ToListAsync();

        //        return new PagedResult<AisleMasterWithShelvesDto>
        //        {
        //            Items = items,
        //            TotalCount = totalCount,
        //            PageNumber = page,
        //            PageSize = pageSize
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        //_logger.LogError(ex, "Error retrieving paginated aisles with shelves");
        //        throw;
        //    }
        //}

        //public async Task<PagedResult<AisleMasterWithShelvesDto>> GetAisleFullDetails(int page = 1, int pageSize = 10, string? search = null, string? status = null)
        //{
        //    try
        //    {
        //        var query = _context.AisleMaster.AsQueryable();

        //        // Apply status filter
        //        if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
        //        {
        //            bool isActive = status.ToLower() == "active";
        //            query = query.Where(a => a.IsActive == isActive);
        //        }

        //        // Apply search filter
        //        if (!string.IsNullOrEmpty(search))
        //        {
        //            search = search.ToLower();
        //            query = query.Where(a =>
        //                a.Name.ToLower().Contains(search) ||
        //                a.Location.ToLower().Contains(search) ||
        //                a.Description.ToLower().Contains(search));
        //        }

        //        // Get total count
        //        var totalCount = await query.CountAsync();

        //        // Apply ordering first
        //        var orderedQuery = query.OrderByDescending(a => a.Id);

        //        // Get all IDs for the page (compatible with SQL Server 2008)
        //        var aisleIds = await orderedQuery
        //            .Select(a => a.Id)
        //            .Skip((page - 1) * pageSize)
        //            .Take(pageSize)
        //            .ToListAsync();

        //        // Now fetch the full data for these IDs
        //        var items = await _context.AisleMaster
        //            .Where(a => aisleIds.Contains(a.Id))
        //            .OrderByDescending(a => a.Id) // Re-apply ordering to maintain order
        //            .Select(aisle => new AisleMasterWithShelvesDto
        //            {
        //                Id = aisle.Id,
        //                Name = aisle.Name,
        //                Description = aisle.Description,
        //                Location = aisle.Location,
        //                Coordinates = aisle.Coordinates,
        //                DeviceName = aisle.DeviceName,
        //                IsActive = aisle.IsActive,
        //                CreatedUser = aisle.CreatedUser,
        //                CreatedDate = aisle.CreatedDate,
        //                UpdatedUser = aisle.UpdatedUser,
        //                UpdatedDate = aisle.UpdatedDate,
        //                Shelves = aisle.Shelves.Select(shelf => new ShelfMasterWithAssignmentsDto
        //                {
        //                    Id = shelf.Id,
        //                    AisleId = shelf.AisleId,
        //                    Name = shelf.Name,
        //                    Location = shelf.Location,
        //                    Coordinates = shelf.Coordinates,
        //                    IPAddress = shelf.IPAddress,
        //                    DeviceName = shelf.DeviceName,
        //                    MACAddress = shelf.MACAddress,
        //                    Description = shelf.Description,
        //                    IsActive = shelf.IsActive,
        //                    CreatedUser = shelf.CreatedUser,
        //                    CreatedDate = shelf.CreatedDate,
        //                    UpdatedUser = shelf.UpdatedUser,
        //                    UpdatedDate = shelf.UpdatedDate,
        //                    Assignments = _context.DeviceTemplateAssignment
        //                        .Where(dta => dta.LocationType == "Shelf" &&
        //                                     dta.LocationId == shelf.Id &&
        //                                     dta.IsActive)
        //                        .Select(dta => new ShelfAssignmentDto
        //                        {
        //                            DeviceTemplateComboId = dta.DeviceTemplateComboId,
        //                            DeviceMAC = dta.DeviceTemplateCombo.Device.MACAddress,
        //                            TemplateName = dta.DeviceTemplateCombo.Template.Name,
        //                            DisplayOrder = dta.DisplayOrder,
        //                            IsActive = dta.IsActive
        //                        })
        //                        .ToList()
        //                }).ToList()
        //            })
        //            .ToListAsync();

        //        return new PagedResult<AisleMasterWithShelvesDto>
        //        {
        //            Items = items,
        //            TotalCount = totalCount,
        //            PageNumber = page,
        //            PageSize = pageSize
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        //_logger.LogError(ex, "Error retrieving paginated aisles with shelves");
        //        throw;
        //    }
        //}

        //public async Task<PagedResult<AisleMasterWithShelvesDto>> GetAisleFullDetails( int page = 1, int pageSize = 10, string? search = null, string? status = null)
        //{
        //    try
        //    {
        //        var query = _context.AisleMaster.AsQueryable();

        //        // Apply status filter
        //        if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
        //        {
        //            bool isActive = status.ToLower() == "active";
        //            query = query.Where(a => a.IsActive == isActive);
        //        }

        //        // Apply search filter
        //        if (!string.IsNullOrEmpty(search))
        //        {
        //            search = search.ToLower();
        //            query = query.Where(a =>
        //                a.Name.ToLower().Contains(search) ||
        //                a.Location.ToLower().Contains(search) ||
        //                a.Description.ToLower().Contains(search));
        //        }

        //        // Get total count
        //        var totalCount = await query.CountAsync();

        //        // Apply pagination and projection
        //        var items = await query
        //            .OrderByDescending(a => a.Id)
        //            .Skip((page - 1) * pageSize)
        //            .Take(pageSize)
        //            .Select(aisle => new AisleMasterWithShelvesDto
        //            {
        //                Id = aisle.Id,
        //                Name = aisle.Name,
        //                Description = aisle.Description,
        //                Location = aisle.Location,
        //                Coordinates = aisle.Coordinates,
        //                DeviceName = aisle.DeviceName,
        //                IsActive = aisle.IsActive,
        //                CreatedUser = aisle.CreatedUser,
        //                CreatedDate = aisle.CreatedDate,
        //                UpdatedUser = aisle.UpdatedUser,
        //                UpdatedDate = aisle.UpdatedDate,
        //                Shelves = aisle.Shelves.Select(shelf => new ShelfMasterWithAssignmentsDto
        //                {
        //                    Id = shelf.Id,
        //                    AisleId = shelf.AisleId,
        //                    Name = shelf.Name,
        //                    Location = shelf.Location,
        //                    Coordinates = shelf.Coordinates,
        //                    IPAddress = shelf.IPAddress,
        //                    DeviceName = shelf.DeviceName,
        //                    MACAddress = shelf.MACAddress,
        //                    Description = shelf.Description,
        //                    IsActive = shelf.IsActive,
        //                    CreatedUser = shelf.CreatedUser,
        //                    CreatedDate = shelf.CreatedDate,
        //                    UpdatedUser = shelf.UpdatedUser,
        //                    UpdatedDate = shelf.UpdatedDate,
        //                    Assignments = _context.DeviceTemplateAssignment
        //                        .Where(dta => dta.LocationType == "Shelf" &&
        //                                     dta.LocationId == shelf.Id &&
        //                                     dta.IsActive)
        //                        .Select(dta => new ShelfAssignmentDto
        //                        {
        //                            DeviceTemplateComboId = dta.DeviceTemplateComboId,
        //                            DeviceMAC = dta.DeviceTemplateCombo.Device.MACAddress,
        //                            TemplateName = dta.DeviceTemplateCombo.Template.Name,
        //                            DisplayOrder = dta.DisplayOrder,
        //                            IsActive = dta.IsActive
        //                        })
        //                        .ToList()
        //                }).ToList()
        //            })
        //            .ToListAsync();

        //        return new PagedResult<AisleMasterWithShelvesDto>
        //        {
        //            Items = items,
        //            TotalCount = totalCount,
        //            PageNumber = page,
        //            PageSize = pageSize
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        //_logger.LogError(ex, "Error retrieving paginated aisles with shelves");
        //        throw;
        //    }
        //}

        public async Task<PagedResult<AisleMasterWithShelvesDto>> GetAisleFullDetails( int page = 1, int pageSize = 10, string? search = null, string? status = null, long? storeId = null)
        {
            try
            {
                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "sp_GetAllAisles";

                // Add parameters
                command.Parameters.Add(new SqlParameter("@PageNumber", page));
                command.Parameters.Add(new SqlParameter("@PageSize", pageSize));
                command.Parameters.Add(new SqlParameter("@SearchTerm", (object)search ?? DBNull.Value));
                command.Parameters.Add(new SqlParameter("@Status", (object)status ?? DBNull.Value));
                command.Parameters.Add(new SqlParameter("@StoreId", storeId.HasValue ? (object)storeId.Value : DBNull.Value));

                using var reader = await command.ExecuteReaderAsync();

                // Read pagination info (first result set)
                int pageNumber = page;
                int pageSizeResult = pageSize;
                int totalCount = 0;
                int totalPages = 0;

                if (await reader.ReadAsync())
                {
                    // Safely read with type checking
                    pageNumber = reader.GetInt32(reader.GetOrdinal("PageNumber"));
                    pageSizeResult = reader.GetInt32(reader.GetOrdinal("PageSize"));
                    totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));

                    // Handle TotalPages which might be returned as FLOAT/DOUBLE
                    var totalPagesValue = reader.GetValue(reader.GetOrdinal("TotalPages"));
                    if (totalPagesValue is double doubleValue)
                    {
                        totalPages = (int)Math.Ceiling(doubleValue);
                    }
                    else if (totalPagesValue is decimal decimalValue)
                    {
                        totalPages = (int)Math.Ceiling((double)decimalValue);
                    }
                    else
                    {
                        totalPages = Convert.ToInt32(totalPagesValue);
                    }
                }

                // Read aisles (second result set)
                await reader.NextResultAsync();
                var aisles = new List<AisleMasterWithShelvesDto>();
                var aisleDict = new Dictionary<long, AisleMasterWithShelvesDto>();

                while (await reader.ReadAsync())
                {
                    var aisle = new AisleMasterWithShelvesDto
                    {
                        Id = SafeGetInt64(reader, "Id"),
                        Name = SafeGetString(reader, "Name"),
                        Description = SafeGetString(reader, "Description"),
                        Location = SafeGetString(reader, "Location"),
                        Coordinates = SafeGetString(reader, "Coordinates"),
                        StoreId = SafeGetInt64(reader,"StoreId"),
                        StoreName = SafeGetString(reader,"StoreName"),
                        IsActive = SafeGetBoolean(reader, "IsActive"),
                        CreatedUser = SafeGetInt32(reader, "CreatedUser"),
                        CreatedDate = SafeGetDateTime(reader, "CreatedDate"),
                        UpdatedUser = SafeGetNullableInt32(reader, "UpdatedUser"),
                        UpdatedDate = SafeGetNullableDateTime(reader, "UpdatedDate"),
                        Shelves = new List<ShelfMasterWithAssignmentsDto>()
                    };

                    aisles.Add(aisle);
                    aisleDict[aisle.Id] = aisle;
                }

                // Read shelves (third result set)
                await reader.NextResultAsync();
                var shelfDict = new Dictionary<long, ShelfMasterWithAssignmentsDto>();

                while (await reader.ReadAsync())
                {
                    var shelfId = SafeGetInt64(reader, "Id");
                    var aisleId = SafeGetInt64(reader, "AisleId");

                    var shelf = new ShelfMasterWithAssignmentsDto
                    {
                        Id = shelfId,
                        AisleId = aisleId,
                        Name = SafeGetString(reader, "Name"),
                        Location = SafeGetString(reader, "Location"),
                        Coordinates = SafeGetString(reader, "Coordinates"),
                        Description = SafeGetString(reader, "Description"),
                        IsActive = SafeGetBoolean(reader, "IsActive"),
                        CreatedUser = SafeGetInt32(reader, "CreatedUser"),
                        CreatedDate = SafeGetDateTime(reader, "CreatedDate"),
                        UpdatedUser = SafeGetNullableInt32(reader, "UpdatedUser"),
                        UpdatedDate = SafeGetNullableDateTime(reader, "UpdatedDate"),
                        Assignments = new List<ShelfAssignmentDto>()
                    };

                    if (aisleDict.ContainsKey(aisleId))
                    {
                        aisleDict[aisleId].Shelves.Add(shelf);
                    }

                    shelfDict[shelfId] = shelf;
                }

                // Read assignments (fourth result set)
                await reader.NextResultAsync();

                while (await reader.ReadAsync())
                {
                    var shelfId = SafeGetInt64(reader, "ShelfId");

                    if (shelfDict.ContainsKey(shelfId))
                    {
                        var assignmentType = SafeGetString(reader, "AssignmentType");
                        var assignment = new ShelfAssignmentDto
                        {
                            AssignmentType = assignmentType,
                            DisplayOrder = SafeGetInt32(reader, "DisplayOrder"),
                            IsActive = SafeGetBoolean(reader, "IsActive")
                        };

                        if (assignmentType == "TEMPLATE")
                        {
                            assignment.DeviceTemplateComboId = SafeGetNullableInt64(reader, "DeviceTemplateComboId");
                            assignment.DeviceId = SafeGetNullableInt64(reader, "DeviceId");
                            assignment.DeviceMAC = SafeGetString(reader, "DeviceMAC");
                            assignment.TemplateId = SafeGetString(reader, "TemplateId");
                            assignment.TemplateName = SafeGetString(reader, "TemplateName");
                        }
                        else if (assignmentType == "MESSAGE")
                        {
                            assignment.DeviceMessageComboId = SafeGetNullableInt64(reader, "DeviceMessageComboId");
                            assignment.MessageDeviceId = SafeGetNullableInt64(reader, "MessageDeviceId");
                            assignment.MessageDeviceMAC = SafeGetString(reader, "MessageDeviceMAC");
                            assignment.MessageId = SafeGetNullableInt64(reader, "MessageId");
                            assignment.MessageTitle = SafeGetString(reader, "MessageTitle");
                            assignment.MessageContentType = SafeGetNullableInt64(reader, "MessageContentType");
                        }

                        shelfDict[shelfId].Assignments.Add(assignment);
                    }
                }

                    return new PagedResult<AisleMasterWithShelvesDto>
                {
                    Items = aisles,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSizeResult,
                    TotalPages = totalPages,
                    //HasPreviousPage = pageNumber > 1,
                    //HasNextPage = pageNumber < totalPages,
                    //HasResults = aisles.Count > 0,
                    SearchTerm = search
                };
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error retrieving paginated aisles with shelves");
                throw;
            }
        }

        private static long? SafeGetNullableInt64(DbDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : Convert.ToInt64(reader.GetValue(ordinal));
        }

        // Helper methods for safe data reading
        private static string SafeGetString(DbDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        private static long SafeGetInt64(DbDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt64(reader.GetValue(ordinal));
        }

        private static int SafeGetInt32(DbDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static int? SafeGetNullableInt32(DbDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static bool SafeGetBoolean(DbDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? false : Convert.ToBoolean(reader.GetValue(ordinal));
        }

        private static DateTime SafeGetDateTime(DbDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? DateTime.MinValue : reader.GetDateTime(ordinal);
        }

        private static DateTime? SafeGetNullableDateTime(DbDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }
        //public async Task<PagedResult<AisleMasterWithShelvesDto>> GetAisleFullDetails( int page = 1, int pageSize = 10, string? search = null, string? status = null)
        //{
        //    try
        //    {
        //        using var connection = _context.Database.GetDbConnection();
        //        await connection.OpenAsync();

        //        using var command = connection.CreateCommand();
        //        command.CommandType = CommandType.StoredProcedure;
        //        command.CommandText = "sp_GetAllAisles";

        //        // Add parameters
        //        command.Parameters.Add(new SqlParameter("@PageNumber", page));
        //        command.Parameters.Add(new SqlParameter("@PageSize", pageSize));
        //        command.Parameters.Add(new SqlParameter("@SearchTerm", (object)search ?? DBNull.Value));
        //        command.Parameters.Add(new SqlParameter("@Status", (object)status ?? DBNull.Value));

        //        using var reader = await command.ExecuteReaderAsync();

        //        // Read pagination info (first result set)
        //        var paginationInfo = new { PageNumber = 1, PageSize = 10, TotalCount = 0, TotalPages = 0 };
        //        if (await reader.ReadAsync())
        //        {
        //            paginationInfo = new
        //            {
        //                PageNumber = reader.GetInt32(reader.GetOrdinal("PageNumber")),
        //                PageSize = reader.GetInt32(reader.GetOrdinal("PageSize")),
        //                TotalCount = reader.GetInt32(reader.GetOrdinal("TotalCount")),
        //                TotalPages = reader.GetInt32(reader.GetOrdinal("TotalPages"))
        //            };
        //        }

        //        // Read aisles (second result set)
        //        await reader.NextResultAsync();
        //        var aisles = new List<AisleMasterWithShelvesDto>();
        //        var aisleDict = new Dictionary<long, AisleMasterWithShelvesDto>();

        //        while (await reader.ReadAsync())
        //        {
        //            var aisle = new AisleMasterWithShelvesDto
        //            {
        //                Id = reader.GetInt64(reader.GetOrdinal("Id")),
        //                Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader.GetString(reader.GetOrdinal("Name")),
        //                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
        //                Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? null : reader.GetString(reader.GetOrdinal("Location")),
        //                Coordinates = reader.IsDBNull(reader.GetOrdinal("Coordinates")) ? null : reader.GetString(reader.GetOrdinal("Coordinates")),
        //                DeviceName = reader.IsDBNull(reader.GetOrdinal("DeviceName")) ? null : reader.GetString(reader.GetOrdinal("DeviceName")),
        //                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
        //                CreatedUser = reader.GetInt32(reader.GetOrdinal("CreatedUser")),
        //                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
        //                UpdatedUser = reader.IsDBNull(reader.GetOrdinal("UpdatedUser")) ? null : reader.GetInt32(reader.GetOrdinal("UpdatedUser")),
        //                UpdatedDate = reader.IsDBNull(reader.GetOrdinal("UpdatedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedDate")),
        //                Shelves = new List<ShelfMasterWithAssignmentsDto>()
        //            };

        //            aisles.Add(aisle);
        //            aisleDict[aisle.Id] = aisle;
        //        }

        //        // Read shelves (third result set)
        //        await reader.NextResultAsync();
        //        var shelfDict = new Dictionary<long, ShelfMasterWithAssignmentsDto>();

        //        while (await reader.ReadAsync())
        //        {
        //            var shelfId = reader.GetInt64(reader.GetOrdinal("Id"));
        //            var aisleId = reader.GetInt64(reader.GetOrdinal("AisleId"));

        //            var shelf = new ShelfMasterWithAssignmentsDto
        //            {
        //                Id = shelfId,
        //                AisleId = aisleId,
        //                Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader.GetString(reader.GetOrdinal("Name")),
        //                Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? null : reader.GetString(reader.GetOrdinal("Location")),
        //                Coordinates = reader.IsDBNull(reader.GetOrdinal("Coordinates")) ? null : reader.GetString(reader.GetOrdinal("Coordinates")),
        //                IPAddress = reader.IsDBNull(reader.GetOrdinal("IPAddress")) ? null : reader.GetString(reader.GetOrdinal("IPAddress")),
        //                DeviceName = reader.IsDBNull(reader.GetOrdinal("DeviceName")) ? null : reader.GetString(reader.GetOrdinal("DeviceName")),
        //                MACAddress = reader.IsDBNull(reader.GetOrdinal("MACAddress")) ? null : reader.GetString(reader.GetOrdinal("MACAddress")),
        //                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
        //                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
        //                CreatedUser = reader.GetInt32(reader.GetOrdinal("CreatedUser")),
        //                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
        //                UpdatedUser = reader.IsDBNull(reader.GetOrdinal("UpdatedUser")) ? null : reader.GetInt32(reader.GetOrdinal("UpdatedUser")),
        //                UpdatedDate = reader.IsDBNull(reader.GetOrdinal("UpdatedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedDate")),
        //                Assignments = new List<ShelfAssignmentDto>()
        //            };

        //            if (aisleDict.ContainsKey(aisleId))
        //            {
        //                aisleDict[aisleId].Shelves.Add(shelf);
        //            }

        //            shelfDict[shelfId] = shelf;
        //        }

        //        // Read assignments (fourth result set)
        //        await reader.NextResultAsync();

        //        while (await reader.ReadAsync())
        //        {
        //            var shelfId = reader.GetInt64(reader.GetOrdinal("ShelfId"));

        //            if (shelfDict.ContainsKey(shelfId))
        //            {
        //                var assignment = new ShelfAssignmentDto
        //                {
        //                    DeviceTemplateComboId = reader.GetInt64(reader.GetOrdinal("DeviceTemplateComboId")),
        //                    DeviceMAC = reader.GetString(reader.GetOrdinal("DeviceMAC")),
        //                    TemplateName = reader.GetString(reader.GetOrdinal("TemplateName")),
        //                    DisplayOrder = reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
        //                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
        //                };

        //                shelfDict[shelfId].Assignments.Add(assignment);
        //            }
        //        }

        //        return new PagedResult<AisleMasterWithShelvesDto>
        //        {
        //            Items = aisles,
        //            TotalCount = paginationInfo.TotalCount,
        //            PageNumber = paginationInfo.PageNumber,
        //            PageSize = paginationInfo.PageSize
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        //_logger.LogError(ex, "Error retrieving paginated aisles with shelves");
        //        throw;
        //    }
        //}
        public async Task<AssignmentIdsResponse> GetUniqueAssignmentIdsAsync()
        {
            var aisleIds = await _context.ProductAssignments
                .Where(pa => pa.IsActive && pa.AisleId > 0)
                .Select(pa => pa.AisleId)
                .Distinct()
                .OrderBy(id => id)
                .ToListAsync();

            var shelfIds = await _context.ProductAssignments
                .Where(pa => pa.IsActive && pa.ShelfId != null && pa.ShelfId > 0)
                .Select(pa => pa.ShelfId.Value)
                .Distinct()
                .OrderBy(id => id)
                .ToListAsync();

            return new AssignmentIdsResponse
            {
                AisleIds = aisleIds,
                ShelfIds = shelfIds
            };
        }

        public async Task<ProductSummaryResponseDto> GetProductSummaryAsync( List<long> aisleIds,List<long> shelfIds)
        {
            string aisleCsv = aisleIds?.Any() == true ? string.Join(",", aisleIds) : null;
            string shelfCsv = shelfIds?.Any() == true ? string.Join(",", shelfIds) : null;

            string json = null;
            var conn = _context.Database.GetDbConnection();
            var wasOpen = conn.State == ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "sp_GetProductSummaryByAislesAndShelves";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@AisleIds", SqlDbType.NVarChar, -1)
                { Value = (object)aisleCsv ?? DBNull.Value });

                cmd.Parameters.Add(new SqlParameter("@ShelfIds", SqlDbType.NVarChar, -1)
                { Value = (object)shelfCsv ?? DBNull.Value });

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                    json = reader["JsonResult"] as string;
            }
            finally
            {
                if (!wasOpen) conn.Close();
            }

            return string.IsNullOrWhiteSpace(json)
                ? new ProductSummaryResponseDto()
                : JsonConvert.DeserializeObject<ProductSummaryResponseDto>(json);
        }

    }

    // DTO for the API response
    public class ShelfMasterWithAssignmentsDto
    {
        public long Id { get; set; }
        public long? AisleId { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string Coordinates { get; set; }
        public string IPAddress { get; set; }
        public string DeviceName { get; set; }
        public string MACAddress { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }

        public int CreatedUser { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? UpdatedUser { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public List<ShelfAssignmentDto> Assignments { get; set; } = new List<ShelfAssignmentDto>();
    }

    public class AisleMasterWithShelvesDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }  
        public string Location { get; set; }
        public string Coordinates { get; set; }
        public long? StoreId { get; set; }
        public string? StoreName { get; set; }
        public bool IsActive { get; set; }

        public int CreatedUser { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? UpdatedUser { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public List<ShelfMasterWithAssignmentsDto> Shelves { get; set; } = new List<ShelfMasterWithAssignmentsDto>();
    }
}
