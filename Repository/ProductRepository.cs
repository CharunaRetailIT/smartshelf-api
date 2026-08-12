using EFCore.BulkExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Data;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Interface;
using TERMS_LOYALTY_API.Models.shelf;
namespace TERMS_LOYALTY_API.Repository
{
    public class ProductRepository : IProduct
    {
        private readonly SmartShelfDbContext _context;
        private readonly IDevice _deviceRepo;
        public ProductRepository(SmartShelfDbContext context, IDevice deviceRepo)
        {
            _context = context;
            _deviceRepo = deviceRepo;
        }

        #region Product Operations

        public async Task<(IEnumerable<ProductViewDto> Products, int TotalCount)> GetAllProductsAsync(int pageNumber = 1, int pageSize = 10, long? storeId = null,long? categoryId = null,long? subcategoryId = null, string searchTerm = "")
        {
            try
            {
                var products = new List<ProductViewDto>();
                int totalCount = 0;

                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("sp_GetAllProducts", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        command.Parameters.Add(new SqlParameter("@PageNumber", pageNumber));
                        command.Parameters.Add(new SqlParameter("@PageSize", pageSize));
                        command.Parameters.Add(new SqlParameter("@SearchTerm",
                            string.IsNullOrWhiteSpace(searchTerm) ? DBNull.Value : searchTerm));
                        command.Parameters.Add(new SqlParameter("@StoreId", storeId.HasValue ? (object)storeId.Value : DBNull.Value));
                        command.Parameters.Add(new SqlParameter("@CategoryId", categoryId.HasValue ? (object)categoryId.Value : DBNull.Value));
                        command.Parameters.Add(new SqlParameter("@SubCategoryId", subcategoryId.HasValue ? (object)subcategoryId.Value : DBNull.Value));
                        command.Parameters.Add(new SqlParameter("@TotalCount", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        });

                        // Execute and read results
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // Read the products
                            while (await reader.ReadAsync())
                            {
                                var product = new ProductViewDto
                                {
                                    Id = reader.GetInt64("Id"),
                                    ProductCode = reader.GetString("ProductCode"),
                                    ProductName = reader.GetString("ProductName"),
                                    BarCode = reader.IsDBNull("BarCode") ? null : reader.GetString("BarCode"),
                                    Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                                    CategoryName = reader.GetString("CategoryName"),
                                    CategoryId = reader.GetInt64("CategoryId"),
                                    SubCategoryName = reader.IsDBNull("SubCategoryName") ? null : reader.GetString("SubCategoryName"),
                                    SubCategoryId = reader.IsDBNull("SubCategoryId") ? null : reader.GetInt64("SubCategoryId"),
                                    Quantity = reader.GetDecimal("Quantity"),
                                    UnitOfMeasure = reader.IsDBNull("UnitOfMeasure") ? null : reader.GetString("UnitOfMeasure"),
                                    CostPrice = reader.GetDecimal("CostPrice"),
                                    SellingPrice = reader.GetDecimal("SellingPrice"),
                                    DiscountPrice = reader.GetDecimal("DiscountPrice"),
                                    DiscountedPrice = reader.GetDecimal("DiscountedPrice"),
                                    DiscountPercentage = reader.GetDecimal("DiscountPercentage"),                   
                                    WholesalePrice = reader.GetDecimal("WholesalePrice"),                   
                                    MaximumPrice = reader.GetDecimal("MaximumPrice"),                   
                                    MinimumPrice = reader.GetDecimal("MinimumPrice"),                   
                                    IsSyncToCloud = reader.GetBoolean("IsSyncToCloud"),
                                    IsActive = reader.GetBoolean("IsActive"),
                                    StoreId = reader.GetInt64("StoreId")
                                };
                                products.Add(product);
                            }
                        }

                        // Get the output parameter value
                        if (command.Parameters["@TotalCount"].Value != DBNull.Value)
                        {
                            totalCount = Convert.ToInt32(command.Parameters["@TotalCount"].Value);
                        }
                    }
                }

                return (products, totalCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while retrieving products: {ex.Message}");
                throw;
            }
        }

        private async Task<int> GetProductsCountAsync(string searchTerm = "")
        {
            var parameters = new[]
            {
                new SqlParameter("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? DBNull.Value : searchTerm)
            };

            // Use standard ADO.NET to execute the stored procedure and get the count
            int count = 0;
            var conn = _context.Database.GetDbConnection();
            await using (conn)
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "sp_GetProductsCount";
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(parameters[0]);

                    var result = await command.ExecuteScalarAsync();
                    if (result != null && int.TryParse(result.ToString(), out int parsed))
                    {
                        count = parsed;
                    }
                }
            }
            return count;
        }

        public async Task<ProductMaster> CreateProductAsync(ProductMaster product)
        {
            try
            {
                // Verify category and subcategory exist and are active
                var category = await _context.ProductCategories
                    .FirstOrDefaultAsync(c => c.Id == product.CategoryId && c.IsActive && c.StoreId == product.StoreId);

                if (category == null)
                    throw new ArgumentException("Invalid category ID");

                if (product.SubCategoryId != null)
                {
                    var subCategory = await _context.ProductSubCategories
                        .FirstOrDefaultAsync(sc => sc.Id == product.SubCategoryId && sc.IsActive && sc.StoreId == product.StoreId);

                    if (subCategory == null)
                        throw new ArgumentException("Invalid subcategory ID");
                
                    // Verify subcategory belongs to category
                    if (subCategory.CategoryId != product.CategoryId)
                        throw new ArgumentException("Subcategory does not belong to the specified category");
                }
                product.CreatedDate = DateTime.Now;
                _context.ProductMaster.Add(product);
                await _context.SaveChangesAsync();
                return product;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error creating product");
                throw;
            }
        }

        public async Task<ProductMaster> UpdateProductAsync(ProductMaster product)
        {
            try
            {
                var existingProduct = await _context.ProductMaster.Where(x => x.StoreId == product.StoreId && product.Id == product.Id).FirstOrDefaultAsync();
                if (existingProduct == null)
                    throw new ArgumentException("Product not found");

                // Verify category and subcategory exist and are active
                var category = await _context.ProductCategories
                    .FirstOrDefaultAsync(c => c.Id == product.CategoryId && c.IsActive && c.StoreId == product.StoreId);

                if (category == null)
                    throw new ArgumentException("Invalid category ID");

                if (product.SubCategoryId != null)
                {
                    var subCategory = await _context.ProductSubCategories
                    .FirstOrDefaultAsync(sc => sc.Id == product.SubCategoryId && sc.IsActive && sc.StoreId == product.StoreId);

                    if (subCategory == null)
                        throw new ArgumentException("Invalid subcategory ID");

                    // Verify subcategory belongs to category
                    if (subCategory.CategoryId != product.CategoryId)
                        throw new ArgumentException("Subcategory does not belong to the specified category");
                }

                existingProduct.ProductCode = product.ProductCode;
                existingProduct.BarCode = product.BarCode;
                existingProduct.ProductName = product.ProductName;
                existingProduct.Quantity = product.Quantity;
                existingProduct.UnitOfMeasure = product.UnitOfMeasure;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.SubCategoryId = product.SubCategoryId;
                existingProduct.CostPrice = product.CostPrice;
                existingProduct.SellingPrice = product.SellingPrice;
                existingProduct.DiscountPrice = product.DiscountPrice;
                existingProduct.DiscountedPrice = product.DiscountedPrice;
                existingProduct.DiscountPercentage = product.DiscountPercentage;
                existingProduct.WholesalePrice = product.WholesalePrice;
                existingProduct.MinimumPrice = product.MinimumPrice;
                existingProduct.MaximumPrice = product.MaximumPrice;
                existingProduct.Description = product.Description;
                existingProduct.IsActive = product.IsActive;
                existingProduct.IsSyncToCloud = product.IsSyncToCloud;
                existingProduct.UpdatedDate = DateTime.Now;
                existingProduct.UpdatedUser = product.UpdatedUser;

                await _context.SaveChangesAsync();
                return existingProduct;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error updating product");
                throw;
            }
        }

        public async Task<ProductMaster> UpdateProductAsync(long id, UpdateProductDto dto)
        {
            try
            {
                var existingProduct = await _context.ProductMaster.Where(x => x.StoreId == dto.StoreId && x.Id == id).FirstOrDefaultAsync();
                if (existingProduct == null)
                    throw new ArgumentException("Product not found");

                // Verify category and subcategory exist and are active
                var category = await _context.ProductCategories
                    .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.IsActive && c.StoreId == dto.StoreId);

                if (category == null)
                    throw new ArgumentException("Invalid category ID");

                if (dto.SubCategoryId != null && dto.SubCategoryId != 0)
                {
                    var subCategory = await _context.ProductSubCategories
                    .FirstOrDefaultAsync(sc => sc.Id == dto.SubCategoryId && sc.IsActive && sc.StoreId == dto.StoreId);

                    if (subCategory == null)
                        throw new ArgumentException("Invalid subcategory ID");

                    // Verify subcategory belongs to category
                    if (subCategory.CategoryId != dto.CategoryId)
                        throw new ArgumentException("Subcategory does not belong to the specified category");
                }

                existingProduct.ProductCode = dto.ProductCode;
                existingProduct.BarCode = dto.BarCode;
                existingProduct.ProductName = dto.ProductName;
                existingProduct.Quantity = dto.Quantity;
                existingProduct.UnitOfMeasure = dto.UnitOfMeasure;
                existingProduct.CategoryId = dto.CategoryId;
                existingProduct.SubCategoryId = dto.SubCategoryId == 0 ? null : dto.SubCategoryId;
                existingProduct.CostPrice = dto.CostPrice;
                existingProduct.SellingPrice = dto.SellingPrice;
                existingProduct.DiscountPrice = dto.DiscountPrice;
                existingProduct.DiscountedPrice = dto.DiscountedPrice;
                existingProduct.DiscountPercentage = dto.DiscountPercentage;
                existingProduct.WholesalePrice = dto.WholesalePrice;
                existingProduct.MinimumPrice = dto.MinimumPrice;
                existingProduct.MaximumPrice = dto.MaximumPrice;
                existingProduct.Description = dto.Description;
                existingProduct.IsActive = dto.IsActive;
                existingProduct.UpdatedDate = DateTime.Now;
                existingProduct.UpdatedUser = dto.UpdatedUser;

                await _context.SaveChangesAsync();
                return existingProduct;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error updating product");
                throw;
            }
        }

        // Saves the product plus every ESL device+template/device+message binding
        // for it in ONE database transaction: if any step fails (bad category,
        // missing device, etc.) everything rolls back - the product is never left
        // half-saved with some assignments applied and others missing.
        public async Task<ProductMaster> SaveProductWithEslAsync(long? productId, ProductEslData productData, List<EslAssignmentIntent> assignments, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                ProductMaster product;

                if (productId.HasValue && productId.Value > 0)
                {
                    var updateDto = new UpdateProductDto
                    {
                        ProductCode = productData.ProductCode,
                        BarCode = productData.BarCode,
                        ProductName = productData.ProductName,
                        CategoryId = productData.CategoryId,
                        SubCategoryId = productData.SubCategoryId ?? 0,
                        Quantity = productData.Quantity,
                        UnitOfMeasure = productData.UnitOfMeasure,
                        CostPrice = productData.CostPrice,
                        SellingPrice = productData.SellingPrice,
                        DiscountPrice = productData.DiscountPrice,
                        DiscountedPrice = productData.DiscountedPrice,
                        DiscountPercentage = productData.DiscountPercentage,
                        WholesalePrice = productData.WholesalePrice,
                        MinimumPrice = productData.MinimumPrice,
                        MaximumPrice = productData.MaximumPrice,
                        Description = productData.Description,
                        IsActive = productData.IsActive,
                        UpdatedUser = userId,
                        StoreId = productData.StoreId,
                    };

                    product = await UpdateProductAsync(productId.Value, updateDto);
                }
                else
                {
                    var exists = await ProductExistsAsync(productData.ProductCode, productData.StoreId);
                    if (exists)
                        throw new ArgumentException("Product with same code already exists.");

                    product = new ProductMaster
                    {
                        ProductCode = productData.ProductCode,
                        BarCode = productData.BarCode,
                        ProductName = productData.ProductName,
                        CategoryId = productData.CategoryId,
                        SubCategoryId = productData.SubCategoryId == 0 ? null : productData.SubCategoryId,
                        Quantity = productData.Quantity,
                        UnitOfMeasure = productData.UnitOfMeasure,
                        CostPrice = productData.CostPrice,
                        SellingPrice = productData.SellingPrice,
                        DiscountPrice = productData.DiscountPrice,
                        DiscountedPrice = productData.DiscountedPrice,
                        DiscountPercentage = productData.DiscountPercentage,
                        WholesalePrice = productData.WholesalePrice,
                        MinimumPrice = productData.MinimumPrice,
                        MaximumPrice = productData.MaximumPrice,
                        Description = productData.Description,
                        IsActive = productData.IsActive,
                        CreatedUser = userId,
                        StoreId = productData.StoreId,
                    };

                    product = await CreateProductAsync(product);
                }

                foreach (var intent in assignments ?? new List<EslAssignmentIntent>())
                {
                    await ApplyEslAssignmentIntentAsync(product.Id, productData.StoreId, userId, intent);
                }

                await transaction.CommitAsync();
                return product;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task ApplyEslAssignmentIntentAsync(long productId, long storeId, int userId, EslAssignmentIntent intent)
        {
            if (intent.IsDeleted)
            {
                if (intent.TemplateAssignmentId.HasValue)
                    await _deviceRepo.RemoveAssignmentAsync(intent.TemplateAssignmentId.Value, userId);
                if (intent.MessageAssignmentId.HasValue)
                    await _deviceRepo.RemoveAssignmentAsync(intent.MessageAssignmentId.Value, userId);
                return;
            }

            if (!string.IsNullOrEmpty(intent.TemplateId))
            {
                // Resolve via (DeviceId, TemplateId) rather than trusting
                // intent.DeviceTemplateComboId - that's just the combo the row
                // originally loaded with, and blindly reusing it here ignored
                // template changes: switching a row's template dropdown kept
                // reusing the old combo (and its old TemplateId) every time,
                // so the label never actually updated to the new template.
                // CreateOrReuseTemplateComboAsync returns the existing combo
                // when device+template already matches, so this is a no-op
                // when nothing changed.
                var (combo, _) = await _deviceRepo.CreateOrReuseTemplateComboAsync(
                    intent.DeviceId, intent.TemplateId, false);

                await AssignComboAsync("TEMPLATE", combo.Id, productId, storeId, userId);
            }
            else if (intent.TemplateAssignmentId.HasValue)
            {
                // Row previously had a template binding that's since been cleared.
                await _deviceRepo.RemoveAssignmentAsync(intent.TemplateAssignmentId.Value, userId);
            }

            if (intent.MessageId.HasValue)
            {
                // Same reasoning as the template combo above - resolve via
                // (DeviceId, MessageId) instead of trusting the possibly-stale
                // intent.DeviceMessageComboId hint, so changing the Message
                // dropdown on an existing row actually takes effect.
                var existingMessageCombo = await _context.DeviceMessageCombos
                    .FirstOrDefaultAsync(c => c.DeviceId == intent.DeviceId && c.MessageId == intent.MessageId.Value);

                long comboId;
                if (existingMessageCombo != null)
                {
                    comboId = existingMessageCombo.Id;
                }
                else
                {
                    var combo = await _deviceRepo.CreateAsync(new CreateDeviceMessageComboDto
                    {
                        DeviceId = intent.DeviceId,
                        MessageId = intent.MessageId.Value,
                        StoreId = storeId,
                        IsActive = intent.IsActive,
                    });
                    comboId = combo.Id;
                }

                await AssignComboAsync("MESSAGE", comboId, productId, storeId, userId);
            }
            else if (intent.MessageAssignmentId.HasValue)
            {
                await _deviceRepo.RemoveAssignmentAsync(intent.MessageAssignmentId.Value, userId);
            }
        }

        private async Task AssignComboAsync(string assignmentType, long comboId, long productId, long storeId, int userId)
        {
            var request = new AssignmentDto.CreateAssignmentRequest
            {
                AssignmentType = assignmentType,
                LocationType = "Product",
                LocationId = productId,
                UserId = userId,
                StoreId = storeId,
            };

            if (assignmentType == "TEMPLATE")
                request.DeviceTemplateComboId = comboId;
            else
                request.DeviceMessageComboId = comboId;

            try
            {
                await _deviceRepo.CreateAssignmentAsync(request);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already assigned"))
            {
                // Re-saving a product whose ESL rows didn't change hits this combo+
                // location pair again - that's not a real failure, so it's a no-op.
            }
        }

        public async Task<bool> DeleteProductAsync(long id, long? storeId)
        {
            try
            {
                var product = await _context.ProductMaster.Where(x => x.Id == id && x.StoreId == storeId).FirstOrDefaultAsync();
                if (product == null)
                    return false;

                _context.ProductMaster.Remove(product);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error deleting product");
                throw;
            }
        }

        public async Task<bool> ProductExistsAsync(string productCode, long? storeId)
        {
            return await _context.ProductMaster
                .AnyAsync(p => p.ProductCode == productCode && p.StoreId == storeId);
        }

        public async Task<ProductMaster> GetProductByCodeAsync(string productCode, long? storeId)
        {
            return await _context.ProductMaster
                .FirstOrDefaultAsync(p => p.ProductCode == productCode && p.StoreId == storeId);
        }

        public async Task<IEnumerable<ProductViewDto>> GetProductsByCategoryAsync(long categoryId, long? storeId)
        {
            var query =
                from pm in _context.ProductMaster
                join c in _context.ProductCategories
                    on pm.CategoryId equals c.Id
                join d in _context.ProductSubCategories
                    on pm.SubCategoryId equals d.Id into subCats
                from d in subCats.DefaultIfEmpty()  
                where pm.CategoryId == categoryId
                   && pm.StoreId == storeId
                select new ProductViewDto
                {
                    Id = pm.Id,
                    ProductCode = pm.ProductCode,
                    ProductName = pm.ProductName,
                    BarCode = pm.BarCode,
                    Quantity = pm.Quantity,
                    UnitOfMeasure = pm.UnitOfMeasure,
                    CategoryName = c.CategoryName,
                    CategoryId = pm.CategoryId,
                    Description = pm.Description,

                    // d can be NULL
                    SubCategoryName = d != null ? d.SubCategoryName : null,
                    SubCategoryId = pm.SubCategoryId,

                    CostPrice = pm.CostPrice,
                    SellingPrice = pm.SellingPrice,
                    DiscountPrice = pm.DiscountPrice,
                    DiscountedPrice = pm.DiscountedPrice,
                    DiscountPercentage = pm.DiscountPercentage,
                    WholesalePrice = pm.WholesalePrice,
                    MaximumPrice = pm.MaximumPrice,
                    MinimumPrice = pm.MinimumPrice,
                    IsActive = pm.IsActive,
                    IsSyncToCloud = pm.IsSyncToCloud,
                };

            return await query.ToListAsync();
        }

        public async Task<ProductViewDto> GetProductByIdAsync(long productId, long? storeId)
        {
            var query =
                     from pm in _context.ProductMaster
                     join c in _context.ProductCategories
                         on pm.CategoryId equals c.Id
                     join d in _context.ProductSubCategories
                         on pm.SubCategoryId equals d.Id into subCats
                     from d in subCats.DefaultIfEmpty()   // ← LEFT JOIN
                     where pm.Id == productId && pm.StoreId == storeId
                     select new ProductViewDto
                     {
                         Id = pm.Id,
                         ProductCode = pm.ProductCode,
                         ProductName = pm.ProductName,
                         BarCode = pm.BarCode,
                         Quantity = pm.Quantity,
                         UnitOfMeasure = pm.UnitOfMeasure,
                         CategoryName = c.CategoryName,
                         CategoryId = pm.CategoryId,
                         Description = pm.Description,
                         SubCategoryName = d != null ? d.SubCategoryName : null,
                         SubCategoryId = pm.SubCategoryId,
                         CostPrice = pm.CostPrice,
                         SellingPrice = pm.SellingPrice,
                         DiscountPrice = pm.DiscountPrice,
                         DiscountedPrice = pm.DiscountedPrice,
                         DiscountPercentage = pm.DiscountPercentage,
                         WholesalePrice = pm.WholesalePrice,
                         MaximumPrice = pm.MaximumPrice,
                         MinimumPrice = pm.MinimumPrice,
                         IsActive = pm.IsActive,
                         IsSyncToCloud = pm.IsSyncToCloud,
                     };


            return await query.FirstOrDefaultAsync();
        }

        //public async Task UpdateProductDeviceAsync(long productId, ProductDeviceUpdateDto deviceInfo, int userId)
        //{
        //    var product = _context.ProductMaster.Where(x => x.Id == productId).FirstOrDefault();
        //    if (product == null)
        //    {
        //        throw new Exception("Product not found");
        //    }
        //    product.IPAddress = deviceInfo.IPAddress;
        //    product.NetworkName = deviceInfo.NetworkName;
        //    product.DeviceName = deviceInfo.DeviceName;
        //    product.MACAddress = deviceInfo.MACAddress;
        //    product.UpdatedDate = DateTime.UtcNow;
        //    product.UpdatedUser = userId;
        //    //_context.ProductMaster.Update(product);
        //    await _context.SaveChangesAsync();
        //}

        public async Task SyncProducts(List<ProductMaster> products, long? storeId)
        {
            var ids = products.Select(p => p.Id).ToList();
            await _context.ProductMaster
                .Where(p => ids.Contains(p.Id) && p.StoreId == storeId)
                .BatchUpdateAsync(new ProductMaster { IsSyncToCloud = true });
        }

        public async Task<string> BuildUnsyncedProductsJsonAsync(string storeId, string? opcode)
        {
            var unsyncedProducts = await _context.ProductMaster
                .Where(p => !p.IsSyncToCloud && p.IsActive)
                .ToListAsync();

            var goodsList = unsyncedProducts.Select(p => new ProductSyncDto
            {
                id = p.Id.ToString(),
                code = p.ProductCode,
                name = p.ProductName,
                quantity = "0",
                specification = p.Description ?? "",
                supplier = "Unknown"
            }).ToList();

            var payload = new ProductSyncRequest
            {
                storeId = storeId,
                goodsList = goodsList,
            };

            return JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        public async Task <IEnumerable<ProductMaster>> GetUnsyncedProductList(long? storeId)
        {
            var unsyncedProducts = await _context.ProductMaster
                .Where(p => !p.IsSyncToCloud && p.IsActive && p.StoreId == storeId)
                .ToListAsync();
           return unsyncedProducts.ToList();
        }

        #endregion

        #region Category Operations

        public async Task<(IEnumerable<ProductCategory> Categories, int TotalCount)> GetActiveCategoriesAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", long? storeId = null)
        {
            try
            {
                var query = _context.ProductCategories
                    .Where(c => c.IsActive && c.StoreId == storeId);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(c =>
                        c.CategoryName.ToLower().Contains(searchTerm) ||
                        c.CategoryCode.ToLower().Contains(searchTerm) ||
                        (c.CategoryDescription != null && c.CategoryDescription.ToLower().Contains(searchTerm))
                    );
                }

                // Get ALL data first (performance issue!)
                var allCategories = await query
                    .OrderByDescending(c => c.CategoryName)
                    .ToListAsync();

                var totalCount = allCategories.Count;

                // Apply pagination in memory
                var categories = allCategories
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return (categories, totalCount);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<ProductCategory> GetCategoryByIdAsync(int id, long? storeId)
        {
            return await _context.ProductCategories
                           .FirstOrDefaultAsync(c => c.Id == id && c.StoreId == storeId);
        }

        public async Task<ProductCategory> CreateCategoryAsync(ProductCategory category)
        {
            try
            {
                category.CreatedDate = DateTime.Now;
                _context.ProductCategories.Add(category);
                await _context.SaveChangesAsync();
                return category;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error creating category");
                throw;
            }
        }

        public async Task<ProductCategory> UpdateCategoryAsync(ProductCategory category)
        {
            try
            {
                var existingCategory = await _context.ProductCategories.FindAsync(category.Id);
                if (existingCategory == null)
                    throw new ArgumentException("Category not found");

                existingCategory.CategoryName = category.CategoryName;
                existingCategory.CategoryCode = category.CategoryCode;
                existingCategory.CategoryDescription = category.CategoryDescription;
                existingCategory.IsActive = category.IsActive;
                existingCategory.UpdatedDate = DateTime.Now;
                existingCategory.UpdatedUser = category.UpdatedUser;

                await _context.SaveChangesAsync();
                return existingCategory;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error updating category");
                throw;
            }
        }

        public async Task<bool> DeleteCategoryAsync(long id, long? storeId)
        {
            try
            {
                var category = await _context.ProductCategories.Where(x => x.Id == id && x.IsActive && x.StoreId == storeId).FirstOrDefaultAsync();
                if (category == null)
                    return false;

                // Check if category has subcategories
                var hasSubCategories = await _context.ProductSubCategories
                    .AnyAsync(sc => sc.CategoryId == id && sc.IsActive && sc.StoreId == storeId);

                if (hasSubCategories)
                    throw new InvalidOperationException("Cannot delete category with active subcategories");

                _context.ProductCategories.Remove(category);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error deleting category");
                throw;
            }
        }

        public async Task<bool> CategoryExistsAsync(string categoryCode, string categoryName, long? storeId)
        {
            return await _context.ProductCategories
                            .AnyAsync(c => c.CategoryCode == categoryCode && c.StoreId == storeId || c.CategoryName == categoryName && c.StoreId == storeId);
        }

        #endregion

        #region SubCategory Operations

        public async Task<(IEnumerable<ProductSubCategory> SubCategories, int TotalCount)> GetActiveSubCategoriesAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", long? categoryId =null, long? storeId = null)
        {
            try
            {
                var query = _context.ProductSubCategories
                    .Where(c => c.IsActive);

                if (storeId.HasValue)
                {
                    query = query.Where(c => c.StoreId == storeId.Value);
                }

                if (categoryId.HasValue)
                {
                    query = query.Where(c => c.CategoryId == categoryId.Value);
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(c =>
                        c.SubCategoryName.ToLower().Contains(searchTerm) ||
                        c.SubCategoryCode.ToLower().Contains(searchTerm) ||
                        (c.SubCategoryDescription != null && c.SubCategoryDescription.ToLower().Contains(searchTerm))
                    );
                }

                // Get ALL data first (performance issue!)
                var allCategories = await query
                    .OrderByDescending(c => c.Id)
                    .ToListAsync();

                var totalCount = allCategories.Count;

                // Apply pagination in memory
                var categories = allCategories
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return (categories, totalCount);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<ProductSubCategory> GetSubCategoryByIdAsync(int id, long? storeId)
        {
            return await _context.ProductSubCategories.FirstOrDefaultAsync(sc => sc.Id == id && sc.StoreId == storeId);
        }

        public async Task<ProductSubCategory> CreateSubCategoryAsync(ProductSubCategory subCategory, long? storeId)
        {
            try
            {
                // Verify category exists
                var category = await _context.ProductCategories
                    .FirstOrDefaultAsync(c => c.Id == subCategory.CategoryId && c.IsActive && c.StoreId == storeId);

                if (category == null)
                    throw new ArgumentException("Invalid category ID");

                subCategory.CreatedDate = DateTime.Now;
                _context.ProductSubCategories.Add(subCategory);
                await _context.SaveChangesAsync();
                return subCategory;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error creating subcategory");
                throw;
            }
        }

        public async Task<ProductSubCategory> UpdateSubCategoryAsync(ProductSubCategory subCategory, long? storeId)
        {
            try
            {
                var existingSubCategory = await _context.ProductSubCategories.Where(x => x.Id == subCategory.Id && x.StoreId == storeId).FirstOrDefaultAsync();
                if (existingSubCategory == null)
                    throw new ArgumentException("SubCategory not found");

                // Verify category exists
                var category = await _context.ProductCategories
                    .FirstOrDefaultAsync(c => c.Id == subCategory.CategoryId && c.IsActive);

                if (category == null)
                    throw new ArgumentException("Invalid category ID");

                existingSubCategory.CategoryId = subCategory.CategoryId;
                existingSubCategory.SubCategoryName = subCategory.SubCategoryName;
                existingSubCategory.SubCategoryCode = subCategory.SubCategoryCode;
                existingSubCategory.SubCategoryDescription = subCategory.SubCategoryDescription;
                existingSubCategory.IsActive = subCategory.IsActive;
                existingSubCategory.UpdatedDate = DateTime.Now;
                existingSubCategory.UpdatedUser = subCategory.UpdatedUser;

                await _context.SaveChangesAsync();
                return existingSubCategory;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error updating subcategory");
                throw;
            }
        }

        public async Task<bool> DeleteSubCategoryAsync(long id, long? storeId)
        {
            try
            {
                var subCategory = await _context.ProductSubCategories.Where(x => x.Id == id && x.StoreId == storeId).FirstOrDefaultAsync();
                if (subCategory == null)
                    return false;

                // Check if subcategory has products
                var hasProducts = await _context.ProductMaster
                    .AnyAsync(p => p.SubCategoryId == id && p.IsActive);

                if (hasProducts)
                    throw new InvalidOperationException("Cannot delete subcategory with active products");

                _context.ProductSubCategories.Remove(subCategory);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error deleting subcategory");
                throw;
            }
        }

        public async Task<bool> SubCategoryExistsAsync(string subCategoryCode, string subCategoryName, long? storeId)
        {
            return await _context.ProductSubCategories
                            .AnyAsync(sc => sc.SubCategoryCode == subCategoryCode && sc.StoreId == storeId || sc.SubCategoryName == subCategoryName && sc.StoreId == storeId);
        }

        public async Task<List<ProductSubCategory>> GetSubCategoryByCategory(int id, long? storeId)
        {
            return await _context.ProductSubCategories.Where(x => x.CategoryId == id && x.IsActive && x.StoreId ==storeId).ToListAsync();
        }

        #endregion
    }
}
