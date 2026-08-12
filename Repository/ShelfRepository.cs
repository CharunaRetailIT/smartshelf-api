using EFCore.BulkExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Data;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Interface;
using TERMS_LOYALTY_API.Models.shelf;
using TERMS_LOYALTY_API.Models.user;

namespace TERMS_LOYALTY_API.Repository
{
    public class ShelfRepository : IShelf
    {
        private readonly SmartShelfDbContext _context;

        public ShelfRepository(SmartShelfDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<ShelfMaster>> GetAllAsync(long? storeId)
        {
            try
            {
                var result = await _context.ShelfMaster.Where(s => s.StoreId == storeId).OrderByDescending(s => s.Id)
                    .ToListAsync();
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

        }

        public async Task<ShelfMaster> GetByIdAsync(long id, long? storeId)
        {
            return await _context.ShelfMaster
                .FirstOrDefaultAsync(s => s.Id == id && s.StoreId == storeId);
        }

        public async Task<ShelfMaster> AddAsync(ShelfMaster shelf)
        {
            try
            {
                _context.ShelfMaster.Add(shelf);
                await _context.SaveChangesAsync();
                return shelf;
            }
            catch (Exception ex) { 
                Console.WriteLine(ex.Message);
                return null;
            }    

        }

        public async Task<ShelfMaster> UpdateAsync(ShelfMaster shelf)
        {
            _context.ShelfMaster.Update(shelf);
            await _context.SaveChangesAsync();
            return shelf;
        }

        public async Task<bool> DeleteAsync(long id, long? storeId)
        {
            var shelf = await _context.ShelfMaster.Where(x => x.StoreId  == storeId).FirstOrDefaultAsync();
            if (shelf == null) return false;

            _context.ShelfMaster.Remove(shelf);
            await _context.SaveChangesAsync();
            return true;
        }

        //public async Task AssignProductAsync(int shelfId, long productId)
        //{
        //    if (!_context.ProductAssignments.Any(sp => sp.ShelfId == shelfId && sp.ProductId == productId))
        //    {
        //        _context.ProductAssignments.Add(new ProductAssignment
        //        {
        //            ShelfId = shelfId,
        //            ProductId = productId
        //        });
        //        await _context.SaveChangesAsync();
        //    }
        //}
        public async Task AssignProductAsync(long shelfId, long productId, long? storeId, int userId)
        {
            try
            {
                var exists = await _context.ProductAssignments
                    .AnyAsync(sp => sp.ShelfId == shelfId && sp.ProductId == productId && sp.IsActive && sp.StoreId == storeId);

                if (!exists)
                {
                    var shelf = _context.ShelfMaster.Where(x => x.Id == shelfId && x.StoreId == storeId);
                    _context.ProductAssignments.Add(new ProductAssignment
                    {
                        ShelfId = shelfId,
                        ProductId = productId,
                        StoreId = storeId,
                        CreatedUser = userId,
                        CreatedDate = DateTime.Now,
                        AisleId = shelf.FirstOrDefault().AisleId ?? 0,
                    });

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error assigning product {productId} to shelf {shelfId}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }


        public async Task RemoveProductAsync(long shelfId, long productId,long? storeId, int userId)
        {
            try
            {
                var shelfProduct = await _context.ProductAssignments
                    .FirstOrDefaultAsync(sp => sp.ShelfId == shelfId && sp.ProductId == productId && sp.IsActive && sp.StoreId == storeId);

                if (shelfProduct != null)
                {
                    shelfProduct.IsActive = false;
                    shelfProduct.UpdatedDate = DateTime.Now;
                    shelfProduct.UpdatedUser = userId;
                    _context.ProductAssignments.Update(shelfProduct);
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

        public async Task<IEnumerable<ProductViewDto>> GetProductsByShelfAsync(long shelfId, long? storeId)
        {
            var query =
                from sp in _context.ProductAssignments
                join pm in _context.ProductMaster
                    on sp.ProductId equals pm.Id

                join c in _context.ProductCategories
                    on pm.CategoryId equals c.Id

                join d in _context.ProductSubCategories
                    on pm.SubCategoryId equals d.Id into subCategories

                from d in subCategories.DefaultIfEmpty()

                where sp.ShelfId == shelfId
                      && sp.IsActive
                      && sp.StoreId == storeId

                select new ProductViewDto
                {
                    Id = pm.Id,
                    ProductCode = pm.ProductCode,
                    CategoryName = c.CategoryName,
                    Description = pm.Description,
                    SubCategoryName = d != null ? d.SubCategoryName : null,
                    SellingPrice = pm.SellingPrice,
                    DiscountPrice = pm.DiscountPrice,
                    ProductName = pm.ProductName,
                    BarCode = pm.BarCode
                };

            return await query.ToListAsync();
        }

        //public async Task<IEnumerable<ProductViewDto>> GetProductsByShelfAsync(long shelfId,long? storeId)
        //{
        //    var query = from sp in _context.ProductAssignments
        //                join pm in _context.ProductMaster on sp.ProductId equals pm.Id
        //                join c in _context.ProductCategories on pm.CategoryId equals c.Id
        //                join d in _context.ProductSubCategories on pm.SubCategoryId equals d.Id
        //                where sp.ShelfId == shelfId && sp.IsActive == true && sp.StoreId == storeId
        //                select new ProductViewDto
        //                {
        //                    Id = pm.Id,
        //                    ProductCode = pm.ProductCode,
        //                    CategoryName = c.CategoryName,
        //                    Description = pm.Description,
        //                    SubCategoryName = d.SubCategoryName,
        //                    SellingPrice = pm.SellingPrice,
        //                    DiscountPrice = pm.DiscountPrice,
        //                    ProductName = pm.ProductName,
        //                    BarCode = pm.BarCode,
        //                };

        //    return await query.ToListAsync();
        //}

        public async Task DeleteShelfAsync(long id, int user,long? storeId)
        {
            try
            {
                var shelf = await _context.ShelfMaster
                    .FirstOrDefaultAsync(s => s.Id == id && s.IsActive && s.StoreId == storeId);

                if (shelf != null)
                {
                    shelf.UpdatedDate = DateTime.UtcNow; 
                    shelf.UpdatedUser = user;
                    shelf.IsActive = false;

                    _context.ShelfMaster.Update(shelf);
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

        public async Task RestoreShelfAsync(long id, int user, long? storeId)
        {
            try
            {
                var shelf = await _context.ShelfMaster
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsActive && s.StoreId == storeId);

                if (shelf != null)
                {
                    shelf.UpdatedDate = DateTime.UtcNow;
                    shelf.UpdatedUser = user;
                    shelf.IsActive = true;
                    _context.ShelfMaster.Update(shelf);
                    await _context.SaveChangesAsync();
                }
            }
            catch (DbUpdateException dbEx)
            {
                throw new Exception("Database error occurred while restoring shelf", dbEx);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while restoring shelf", ex);
            }
        }

        public async Task<ShelfFullDetails> GetShelfWithAssignmentsAsync(long id, long? storeId)
        {
            try
            {
                // Get shelf with aisle information
                var shelf = await _context.ShelfMaster
                    .Include(s => s.Aisle)
                    .FirstOrDefaultAsync(s => s.Id == id && s.StoreId == storeId);

                if (shelf == null) return null;

                // Get device assignments for this shelf - both TEMPLATE and MESSAGE types
                var assignments = await _context.DeviceAssignment
                    .Where(dta => dta.LocationType == "Shelf" &&
                                 dta.LocationId == id &&
                                 dta.IsActive && dta.StoreId == storeId)
                    .Select(dta => new
                    {
                        // Common fields
                        dta.AssignmentType,
                        dta.DeviceTemplateComboId,
                        dta.DeviceMessageComboId,
                        dta.DisplayOrder,
                        dta.IsActive,

                        // Template assignment fields (only populated for TEMPLATE type)
                        TemplateDeviceId = dta.DeviceTemplateCombo != null ? dta.DeviceTemplateCombo.DeviceId : (long?)null,
                        TemplateDeviceMAC = dta.DeviceTemplateCombo != null && dta.DeviceTemplateCombo.Device != null ?
                            dta.DeviceTemplateCombo.Device.MACAddress : null,
                        TemplateId = dta.DeviceTemplateCombo != null ? dta.DeviceTemplateCombo.TemplateId : (string?)null,
                        TemplateName = dta.DeviceTemplateCombo != null && dta.DeviceTemplateCombo.Template != null ?
                            dta.DeviceTemplateCombo.Template.Name : null,

                        // Message assignment fields (only populated for MESSAGE type)
                        MessageDeviceId = dta.DeviceMessageCombo != null ? dta.DeviceMessageCombo.DeviceId : (long?)null,
                        MessageDeviceMAC = dta.DeviceMessageCombo != null && dta.DeviceMessageCombo.Device != null ?
                            dta.DeviceMessageCombo.Device.MACAddress : null,
                        MessageId = dta.DeviceMessageCombo != null ? dta.DeviceMessageCombo.MessageId : (long?)null,
                        MessageTitle = dta.DeviceMessageCombo != null && dta.DeviceMessageCombo.Message != null ?
                            dta.DeviceMessageCombo.Message.Title : null,
                        MessageContentType = dta.DeviceMessageCombo != null && dta.DeviceMessageCombo.Message != null ?
                            dta.DeviceMessageCombo.Message.ContentType : (long?)null
                    })
                    .OrderBy(a => a.DisplayOrder)
                    .ToListAsync();

                var assignmentDtos = assignments.Select(a => new ShelfAssignmentDto
                {
                    AssignmentType = a.AssignmentType ?? "TEMPLATE",

                    // DeviceId should be based on AssignmentType
                    DeviceId = a.AssignmentType == "MESSAGE" ? a.MessageDeviceId : a.TemplateDeviceId,

                    // DeviceMAC should also be based on AssignmentType
                    DeviceMAC = a.AssignmentType == "MESSAGE" ?
        a.MessageDeviceMAC ?? string.Empty :
        a.TemplateDeviceMAC ?? string.Empty,

                    // Template assignment fields
                    DeviceTemplateComboId = a.DeviceTemplateComboId,
                    TemplateId = a.TemplateId?.ToString(),
                    TemplateName = a.TemplateName ?? string.Empty,

                    // Message assignment fields
                    DeviceMessageComboId = a.DeviceMessageComboId,
                    MessageId = a.MessageId,
                    MessageTitle = a.MessageTitle ?? string.Empty,
                    MessageContentType = a.MessageContentType,

                    // Common fields
                    DisplayOrder = a.DisplayOrder,
                    IsActive = a.IsActive
                }).ToList();

                //// Map assignments to DTO
                //var assignmentDtos = assignments.Select(a => new ShelfAssignmentDto
                //{
                //    AssignmentType = a.AssignmentType ?? "TEMPLATE", // Default to TEMPLATE if null

                //    // Template assignment fields
                //    DeviceTemplateComboId = a.DeviceTemplateComboId,
                //    DeviceId = a.TemplateDeviceId,
                //    DeviceMAC = a.TemplateDeviceMAC ?? string.Empty,
                //    TemplateId = a.TemplateId?.ToString(),
                //    TemplateName = a.TemplateName ?? string.Empty,

                //    // Message assignment fields
                //    DeviceMessageComboId = a.DeviceMessageComboId,
                //    MessageDeviceId = a.MessageDeviceId,
                //    MessageDeviceMAC = a.MessageDeviceMAC ?? string.Empty,
                //    MessageId = a.MessageId,
                //    MessageTitle = a.MessageTitle ?? string.Empty,
                //    MessageContentType = a.MessageContentType,

                //    // Common fields
                //    DisplayOrder = a.DisplayOrder,
                //    IsActive = a.IsActive
                //}).ToList();

                // Alternative approach using separate queries for better performance
                // This avoids potential null reference issues with navigation properties
                if (!assignments.Any())
                {
                    // If no assignments found, we can still return empty list
                    assignmentDtos = new List<ShelfAssignmentDto>();
                }

                // Map to DTO
                var result = new ShelfFullDetails
                {
                    Id = shelf.Id,
                    AisleId = shelf.AisleId,
                    Name = shelf.Name ?? string.Empty,
                    Location = shelf.Location ?? string.Empty,
                    Coordinates = shelf.Coordinates ?? string.Empty,
                    Description = shelf.Description ?? string.Empty,
                    IsActive = shelf.IsActive,
                    CreatedUser = shelf.CreatedUser,
                    CreatedDate = shelf.CreatedDate,
                    UpdatedUser = shelf.UpdatedUser,
                    UpdatedDate = shelf.UpdatedDate,
                    Assignments = assignmentDtos
                };

                // Add aisle info if available
                if (shelf.Aisle != null)
                {
                    result.Aisle = new AisleBasicInfoDto
                    {
                        Id = shelf.Aisle.Id,
                        Name = shelf.Aisle.Name ?? string.Empty,
                        Location = shelf.Aisle.Location ?? string.Empty,
                        IsActive = shelf.Aisle.IsActive
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error retrieving shelf with assignments for ID {ShelfId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<ShelfMaster>> GetUnsyncedShelf(long? storeId)
        {
            var unsyncedProducts = await _context.ShelfMaster
                .Where(p => !p.IsSyncToCloud && p.IsActive && p.StoreId == storeId)
                .ToListAsync();
            return unsyncedProducts.ToList();
        }

        public async Task SyncProducts(List<ShelfMaster> shelfs, long? storeId)
        {
            var ids = shelfs.Select(p => p.Id).ToList();
            await _context.ShelfMaster
                .Where(p => ids.Contains(p.Id) && p.StoreId == storeId)
                .BatchUpdateAsync(new ShelfMaster { IsSyncToCloud = true });
        }



        //public async Task<ShelfFullDetails> GetShelfWithAssignmentsAsync(long id)
        //{
        //    try
        //    {
        //        // Get shelf with aisle information
        //        var shelf = await _context.ShelfMaster
        //            .Include(s => s.Aisle)
        //            .FirstOrDefaultAsync(s => s.Id == id);

        //        if (shelf == null) return null;

        //        // Get device assignments for this shelf
        //        var assignments = await _context.DeviceAssignment
        //            .Where(dta => dta.LocationType == "Shelf" &&
        //                         dta.LocationId == id &&
        //                         dta.IsActive)
        //            .Include(dta => dta.DeviceTemplateCombo)
        //                .ThenInclude(dtc => dtc.Device)
        //            .Include(dta => dta.DeviceTemplateCombo)
        //                .ThenInclude(dtc => dtc.Template)
        //            .Select(dta => new ShelfAssignmentDto
        //            {
        //                DeviceTemplateComboId = dta.DeviceTemplateComboId,
        //                DeviceId = dta.DeviceTemplateCombo.DeviceId,
        //                DeviceMAC = dta.DeviceTemplateCombo.Device.MACAddress ?? string.Empty,
        //                TemplatedId = dta.DeviceTemplateCombo.TemplateId,
        //                TemplateName = dta.DeviceTemplateCombo.Template.Name ?? string.Empty,
        //                DisplayOrder = dta.DisplayOrder,
        //                IsActive = dta.IsActive
        //            })
        //            .OrderBy(a => a.DisplayOrder)
        //            .ToListAsync();

        //        // Map to DTO
        //        var result = new ShelfFullDetails
        //        {
        //            Id = shelf.Id,
        //            AisleId = shelf.AisleId,
        //            Name = shelf.Name ?? string.Empty,
        //            Location = shelf.Location ?? string.Empty,
        //            Coordinates = shelf.Coordinates ?? string.Empty,
        //            IPAddress = shelf.IPAddress ?? string.Empty,
        //            DeviceName = shelf.DeviceName ?? string.Empty,
        //            MACAddress = shelf.MACAddress ?? string.Empty,
        //            Description = shelf.Description ?? string.Empty,
        //            IsActive = shelf.IsActive,
        //            CreatedUser = shelf.CreatedUser,
        //            CreatedDate = shelf.CreatedDate,
        //            UpdatedUser = shelf.UpdatedUser,
        //            UpdatedDate = shelf.UpdatedDate,
        //            Assignments = assignments
        //        };

        //        // Add aisle info if available
        //        if (shelf.Aisle != null)
        //        {
        //            result.Aisle = new AisleBasicInfoDto
        //            {
        //                Id = shelf.Aisle.Id,
        //                Name = shelf.Aisle.Name ?? string.Empty,
        //                Location = shelf.Aisle.Location ?? string.Empty,
        //                IsActive = shelf.Aisle.IsActive
        //            };
        //        }

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        //_logger.LogError(ex, "Error retrieving shelf with assignments for ID {ShelfId}", id);
        //        throw;
        //    }
        //}

        //public IEnumerable<ShelfProduct> GetAll(string DBConnectionString)
        //{
        //    try
        //    {
        //        var contextOptions = new DbContextOptionsBuilder<DatabaseContext>()
        //            .UseSqlServer(DBConnectionString)
        //            .Options;

        //        using (var entities = new DatabaseContext(contextOptions))
        //        {
        //            var result = entities.ShelfProducts
        //                .ToList();
        //            return result;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        throw ex;
        //    }
        //}

        //public ShelfMaster GetById(int id, string DBConnectionString)
        //{
        //    try
        //    {
        //        var contextOptions = new DbContextOptionsBuilder<DatabaseContext>()
        //            .UseSqlServer(DBConnectionString)
        //            .Options;

        //        using (var entities = new DatabaseContext(contextOptions))
        //        {
        //            return entities.ShelfMaster
        //                .Include(s => s.ShelfProducts)
        //                .ThenInclude(sp => sp.Product)
        //                .FirstOrDefault(s => s.ShelfID == id);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public ShelfMaster Add(ShelfMaster shelf, string DBConnectionString)
        //{
        //    try
        //    {
        //        var contextOptions = new DbContextOptionsBuilder<DatabaseContext>()
        //            .UseSqlServer(DBConnectionString)
        //            .Options;

        //        using (var entities = new DatabaseContext(contextOptions))
        //        {
        //            entities.ShelfMaster.Add(shelf);
        //            entities.SaveChanges();
        //            return shelf;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public ShelfMaster Update(ShelfMaster shelf, string DBConnectionString)
        //{
        //    try
        //    {
        //        var contextOptions = new DbContextOptionsBuilder<DatabaseContext>()
        //            .UseSqlServer(DBConnectionString)
        //            .Options;

        //        using (var entities = new DatabaseContext(contextOptions))
        //        {
        //            entities.ShelfMaster.Update(shelf);
        //            entities.SaveChanges();
        //            return shelf;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public bool Delete(int id, string DBConnectionString)
        //{
        //    try
        //    {
        //        var contextOptions = new DbContextOptionsBuilder<DatabaseContext>()
        //            .UseSqlServer(DBConnectionString)
        //            .Options;

        //        using (var entities = new DatabaseContext(contextOptions))
        //        {
        //            var shelf = entities.ShelfMaster.Find(id);
        //            if (shelf == null) return false;

        //            entities.ShelfMaster.Remove(shelf);
        //            entities.SaveChanges();
        //            return true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public void AssignProduct(int shelfId, long productId, string DBConnectionString)
        //{
        //    try
        //    {
        //        var contextOptions = new DbContextOptionsBuilder<DatabaseContext>()
        //            .UseSqlServer(DBConnectionString)
        //            .Options;

        //        using (var entities = new DatabaseContext(contextOptions))
        //        {
        //            if (!entities.ShelfProducts.Any(sp => sp.ShelfID == shelfId && sp.ProductID == productId))
        //            {
        //                entities.ShelfProducts.Add(new ShelfProduct
        //                {
        //                    ShelfID = shelfId,
        //                    ProductID = productId
        //                });
        //                entities.SaveChanges();
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public void RemoveProduct(int shelfId, int productId, string DBConnectionString)
        //{
        //    try
        //    {
        //        var contextOptions = new DbContextOptionsBuilder<DatabaseContext>()
        //            .UseSqlServer(DBConnectionString)
        //            .Options;

        //        using (var entities = new DatabaseContext(contextOptions))
        //        {
        //            var shelfProduct = entities.ShelfProducts
        //                .FirstOrDefault(sp => sp.ShelfID == shelfId && sp.ProductID == productId);

        //            if (shelfProduct != null)
        //            {
        //                entities.ShelfProducts.Remove(shelfProduct);
        //                entities.SaveChanges();
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public IEnumerable<ProductViewDto> GetProductsByShelf(int shelfId, string DBConnectionString)
        //{
        //    try
        //    {
        //        var contextOptions = new DbContextOptionsBuilder<DatabaseContext>()
        //            .UseSqlServer(DBConnectionString)
        //            .Options;

        //        using (var entities = new DatabaseContext(contextOptions))
        //        {
        //            var query = from sp in entities.ShelfProducts
        //                        join psm in entities.InvProductStockMaster on sp.ProductID equals psm.ProductID
        //                        join pm in entities.InvProductMaster on psm.ProductID equals pm.InvProductMasterID
        //                        join c in entities.InvCategory on pm.CategoryID equals c.InvCategoryID
        //                        join d in entities.InvDepartment on pm.DepartmentID equals d.InvDepartmentID
        //                        join l in entities.Location on psm.LocationID equals l.LocationID
        //                        where sp.ShelfID == shelfId
        //                        select new ProductViewDto
        //                        {
        //                            ProductID = psm.ProductID,
        //                            CategoryCode = c.CategoryCode,
        //                            CategoryName = c.CategoryName,
        //                            DepartmentCode = d.DepartmentCode,
        //                            DepartmentName = d.DepartmentName,
        //                            LocationID = psm.LocationID,
        //                            LocationName = l.LocationName,
        //                            ProductName = pm.ProductName,
        //                            Barcode = pm.BarCode,
        //                            Price = psm.SellingPrice
        //                        };

        //            return query.ToList();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }

    }
}
