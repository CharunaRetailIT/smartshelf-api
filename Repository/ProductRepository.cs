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
                // Only when the caller actually sent it - see UpdateProductDto.
                if (dto.IsActive.HasValue)
                    existingProduct.IsActive = dto.IsActive.Value;
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

        #region External integration (lookup + bulk)

        public async Task<ProductDetailDto> GetProductDetailAsync(long? productId, string productCode, long storeId)
        {
            var query =
                from pm in _context.ProductMaster
                join c in _context.ProductCategories on pm.CategoryId equals c.Id into cats
                from c in cats.DefaultIfEmpty()
                join sc in _context.ProductSubCategories on pm.SubCategoryId equals sc.Id into subCats
                from sc in subCats.DefaultIfEmpty()
                join s in _context.StoreMaster on pm.StoreId equals s.Id into stores
                from s in stores.DefaultIfEmpty()
                where pm.StoreId == storeId
                   && (productId.HasValue ? pm.Id == productId.Value : pm.ProductCode == productCode)
                select new ProductDetailDto
                {
                    Id = pm.Id,
                    ProductCode = pm.ProductCode,
                    BarCode = pm.BarCode,
                    ProductName = pm.ProductName,
                    CategoryId = pm.CategoryId,
                    CategoryName = c != null ? c.CategoryName : null,
                    SubCategoryId = pm.SubCategoryId,
                    SubCategoryName = sc != null ? sc.SubCategoryName : null,
                    Quantity = pm.Quantity,
                    UnitOfMeasure = pm.UnitOfMeasure,
                    CostPrice = pm.CostPrice,
                    SellingPrice = pm.SellingPrice,
                    DiscountPrice = pm.DiscountPrice,
                    DiscountedPrice = pm.DiscountedPrice,
                    DiscountPercentage = pm.DiscountPercentage,
                    WholesalePrice = pm.WholesalePrice,
                    MinimumPrice = pm.MinimumPrice,
                    MaximumPrice = pm.MaximumPrice,
                    Description = pm.Description,
                    IsActive = pm.IsActive,
                    IsSyncToCloud = pm.IsSyncToCloud,
                    StoreId = pm.StoreId,
                    StoreName = s != null ? s.StoreName : null,
                    CreatedDate = pm.CreatedDate,
                    UpdatedDate = pm.UpdatedDate,
                };

            var product = await query.FirstOrDefaultAsync();
            if (product == null)
                return null;

            product.EslDevices = await GetEslDevicesForProductAsync(product.Id, storeId);
            return product;
        }

        /// <summary>
        /// Walks DeviceAssignment (LocationType 'Product') out to the device,
        /// template and message, and collapses the rows per device: a label bound
        /// with both a template and a message is one entry with both populated,
        /// not two near-duplicate entries.
        /// </summary>
        public async Task<List<ProductEslDto>> GetEslDevicesForProductAsync(long productId, long storeId)
        {
            var assignments = await _context.DeviceAssignment
                .Where(a => a.LocationType == "Product" && a.LocationId == productId
                            && a.StoreId == storeId && a.IsActive)
                .Include(a => a.DeviceTemplateCombo).ThenInclude(c => c.Device).ThenInclude(d => d.Status)
                .Include(a => a.DeviceTemplateCombo).ThenInclude(c => c.Template)
                .Include(a => a.DeviceMessageCombo).ThenInclude(c => c.Device).ThenInclude(d => d.Status)
                .Include(a => a.DeviceMessageCombo).ThenInclude(c => c.Message)
                .OrderBy(a => a.DisplayOrder)
                .ToListAsync();

            var byDevice = new Dictionary<long, ProductEslDto>();

            foreach (var assignment in assignments)
            {
                var isTemplate = assignment.DeviceTemplateComboId.HasValue;
                var device = isTemplate
                    ? assignment.DeviceTemplateCombo?.Device
                    : assignment.DeviceMessageCombo?.Device;

                // A combo whose device row was hard-deleted leaves a dangling
                // assignment; there is nothing meaningful to report for it.
                if (device == null)
                    continue;

                if (!byDevice.TryGetValue(device.Id, out var entry))
                {
                    entry = new ProductEslDto
                    {
                        DeviceId = device.Id,
                        Mac = device.MACAddress,
                        DeviceName = device.Name,
                        DeviceType = device.DeviceType,
                        Status = device.Status?.Name ?? string.Empty,
                        Battery = device.Battery,
                        IsOnline = device.IsOnline,
                        LastSeen = device.LastSeen,
                        DisplayOrder = assignment.DisplayOrder,
                    };
                    byDevice[device.Id] = entry;
                }

                if (isTemplate)
                {
                    entry.TemplateAssignmentId = assignment.Id;
                    entry.DeviceTemplateComboId = assignment.DeviceTemplateComboId;
                    entry.TemplateId = assignment.DeviceTemplateCombo?.TemplateId;
                    entry.TemplateName = assignment.DeviceTemplateCombo?.Template?.Name;
                }
                else
                {
                    entry.MessageAssignmentId = assignment.Id;
                    entry.DeviceMessageComboId = assignment.DeviceMessageComboId;
                    entry.MessageId = assignment.DeviceMessageCombo?.MessageId;
                    entry.MessageName = assignment.DeviceMessageCombo?.Message?.Title;
                }
            }

            return byDevice.Values.ToList();
        }

        /// <summary>
        /// Resolves a category by name for callers (POS, ERP) that don't know our
        /// internal ids. Returns null when the name doesn't match an active
        /// category in the store, so the caller can fail that row with a reason.
        /// </summary>
        private async Task<long?> ResolveCategoryIdAsync(long? categoryId, string categoryName, long storeId)
        {
            if (categoryId.HasValue && categoryId.Value > 0)
                return categoryId;

            if (string.IsNullOrWhiteSpace(categoryName))
                return null;

            var match = await _context.ProductCategories
                .FirstOrDefaultAsync(c => c.CategoryName == categoryName && c.IsActive && c.StoreId == storeId);

            return match?.Id;
        }

        private async Task<long?> ResolveSubCategoryIdAsync(long? subCategoryId, string subCategoryName, long categoryId, long storeId)
        {
            if (subCategoryId.HasValue && subCategoryId.Value > 0)
                return subCategoryId;

            if (string.IsNullOrWhiteSpace(subCategoryName))
                return null;

            var match = await _context.ProductSubCategories
                .FirstOrDefaultAsync(sc => sc.SubCategoryName == subCategoryName && sc.IsActive
                                           && sc.StoreId == storeId && sc.CategoryId == categoryId);

            return match?.Id;
        }

        /// <summary>
        /// Creates one row of a bulk payload, including its optional ESL bindings,
        /// in its own transaction. Per-row rather than per-batch on purpose: a bad
        /// row rolls back only itself, so the rest of the payload still lands.
        /// </summary>
        public async Task<ProductMaster> BulkCreateProductAsync(BulkProductCreateItem item, long storeId, int userId)
        {
            if (string.IsNullOrWhiteSpace(item.ProductCode))
                throw new ArgumentException("ProductCode is required.");
            if (string.IsNullOrWhiteSpace(item.ProductName))
                throw new ArgumentException("ProductName is required.");

            var categoryId = await ResolveCategoryIdAsync(item.CategoryId, item.CategoryName, storeId);
            if (!categoryId.HasValue)
                throw new ArgumentException($"Category not found (CategoryId={item.CategoryId}, CategoryName='{item.CategoryName}').");

            var subCategoryId = await ResolveSubCategoryIdAsync(item.SubCategoryId, item.SubCategoryName, categoryId.Value, storeId);
            if (!subCategoryId.HasValue && !string.IsNullOrWhiteSpace(item.SubCategoryName))
                throw new ArgumentException($"SubCategory '{item.SubCategoryName}' not found under the resolved category.");

            var productData = new ProductEslData
            {
                ProductCode = item.ProductCode,
                BarCode = item.BarCode,
                ProductName = item.ProductName,
                CategoryId = categoryId.Value,
                SubCategoryId = subCategoryId,
                Quantity = item.Quantity,
                UnitOfMeasure = item.UnitOfMeasure ?? string.Empty,
                CostPrice = item.CostPrice,
                SellingPrice = item.SellingPrice,
                DiscountPrice = item.DiscountPrice,
                DiscountedPrice = item.DiscountedPrice,
                DiscountPercentage = item.DiscountPercentage,
                WholesalePrice = item.WholesalePrice,
                MinimumPrice = item.MinimumPrice,
                MaximumPrice = item.MaximumPrice,
                Description = item.Description,
                IsActive = item.IsActive,
                StoreId = storeId,
            };

            // Reuses the same transactional path as POST /with-esl, so a bulk row
            // and a single create cannot drift apart in behaviour.
            return await SaveProductWithEslAsync(null, productData, item.EslAssignments, userId);
        }

        /// <summary>
        /// Applies one row of a bulk update. Fields left null in the payload keep
        /// their current value - a price-only feed must not blank descriptions.
        /// </summary>
        public async Task<ProductMaster> BulkUpdateProductAsync(BulkProductUpdateItem item, long storeId, int userId)
        {
            if (!item.Id.HasValue && string.IsNullOrWhiteSpace(item.ProductCode))
                throw new ArgumentException("Either Id or ProductCode is required to match the product.");

            var product = item.Id.HasValue
                ? await _context.ProductMaster.FirstOrDefaultAsync(p => p.Id == item.Id.Value && p.StoreId == storeId)
                : await _context.ProductMaster.FirstOrDefaultAsync(p => p.ProductCode == item.ProductCode && p.StoreId == storeId);

            if (product == null)
                throw new ArgumentException(item.Id.HasValue
                    ? $"Product Id {item.Id.Value} not found in store {storeId}."
                    : $"Product code '{item.ProductCode}' not found in store {storeId}.");

            if (item.CategoryId.HasValue || !string.IsNullOrWhiteSpace(item.CategoryName))
            {
                var categoryId = await ResolveCategoryIdAsync(item.CategoryId, item.CategoryName, storeId);
                if (!categoryId.HasValue)
                    throw new ArgumentException($"Category not found (CategoryId={item.CategoryId}, CategoryName='{item.CategoryName}').");
                product.CategoryId = categoryId.Value;
            }

            if (item.SubCategoryId.HasValue || !string.IsNullOrWhiteSpace(item.SubCategoryName))
            {
                var subCategoryId = await ResolveSubCategoryIdAsync(item.SubCategoryId, item.SubCategoryName, product.CategoryId, storeId);
                if (!subCategoryId.HasValue)
                    throw new ArgumentException($"SubCategory not found (SubCategoryId={item.SubCategoryId}, SubCategoryName='{item.SubCategoryName}').");

                var subCategory = await _context.ProductSubCategories
                    .FirstOrDefaultAsync(sc => sc.Id == subCategoryId.Value && sc.IsActive && sc.StoreId == storeId);
                if (subCategory == null)
                    throw new ArgumentException("Invalid subcategory ID");
                if (subCategory.CategoryId != product.CategoryId)
                    throw new ArgumentException("Subcategory does not belong to the specified category");

                product.SubCategoryId = subCategoryId;
            }

            if (item.BarCode != null) product.BarCode = item.BarCode;
            if (item.ProductName != null) product.ProductName = item.ProductName;
            if (item.UnitOfMeasure != null) product.UnitOfMeasure = item.UnitOfMeasure;
            if (item.Description != null) product.Description = item.Description;

            if (item.Quantity.HasValue) product.Quantity = item.Quantity.Value;
            if (item.CostPrice.HasValue) product.CostPrice = item.CostPrice.Value;
            if (item.SellingPrice.HasValue) product.SellingPrice = item.SellingPrice.Value;
            if (item.DiscountPrice.HasValue) product.DiscountPrice = item.DiscountPrice.Value;
            if (item.DiscountedPrice.HasValue) product.DiscountedPrice = item.DiscountedPrice.Value;
            if (item.DiscountPercentage.HasValue) product.DiscountPercentage = item.DiscountPercentage.Value;
            if (item.WholesalePrice.HasValue) product.WholesalePrice = item.WholesalePrice.Value;
            if (item.MinimumPrice.HasValue) product.MinimumPrice = item.MinimumPrice.Value;
            if (item.MaximumPrice.HasValue) product.MaximumPrice = item.MaximumPrice.Value;
            if (item.IsActive.HasValue) product.IsActive = item.IsActive.Value;

            product.UpdatedDate = DateTime.Now;
            product.UpdatedUser = userId;

            await _context.SaveChangesAsync();
            return product;
        }

        #endregion
    }
}
