using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.DTOs;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Interface;
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Models.shelf;
using TERMS_LOYALTY_API.Services;
using TERMS_MOBILE_WEB_API.Controllers;
using TERMS_MOBILE_WEB_API.Models;
using static TERMS_LOYALTY_API.DTOs.shelf.MiewProduct;

namespace TERMS_LOYALTY_API.Controllers
{
    [Route("api/products")]
    [ApiController]
    [Authorize]

    public class ProductsController : ControllerBase
    {
        private readonly IProduct _productRepo;
        private readonly IStore _storeRepo;
        private readonly ILogger<ProductsController> _logger;
        private readonly MinewCloudService _minewCloudService;

        public ProductsController(ILogger<ProductsController> logger, IProduct product, IStore store, MinewCloudService minewService)
        {
            _logger = logger;
            _productRepo = product;
            _minewCloudService= minewService;
            _storeRepo = store;
        }

        #region Product Handlers
        /// <summary>
        /// Retrieves all products.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(HttpResponseData<PagedResult<ProductViewDto>>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<PagedResult<ProductViewDto>>), 404)] 
        [ProducesResponseType(typeof(HttpResponseData<PagedResult<ProductViewDto>>), 500)]
        public async Task<IActionResult> GetAllAsync([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] long? storeId = null, [FromQuery] long? categoryId = null, [FromQuery] long? subcategoryId = null, [FromQuery] string searchTerm = "")
        {
            var response = new HttpResponseData<PagedResult<ProductViewDto>>();
            try
            {
                var (products, totalCount) = await _productRepo.GetAllProductsAsync(pageNumber, pageSize, storeId,categoryId,subcategoryId, searchTerm);

                var result = new PagedResult<ProductViewDto>
                {
                    Items = products.ToList(),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    SearchTerm = searchTerm 
                };

                response.Success = true;
                response.Message = totalCount == 0 && !string.IsNullOrEmpty(searchTerm)
                    ? $"No products found matching '{searchTerm}'"
                    : "Products retrieved successfully.";
                response.Result = result;
                response.ResponsCode = totalCount == 0 ? 404 : 200;

                return totalCount == 0 ? NotFound(response) : Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products with pagination");
                response.Success = false;
                response.Message = "Failed to retrieve products.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Create a new product.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPost("product")]
        [ProducesResponseType(typeof(HttpResponseData<ProductResponseDto>), 201)]
        [ProducesResponseType(typeof(HttpResponseData<ProductResponseDto>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<ProductResponseDto>), 500)]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto createDto)
        {
            var response = new HttpResponseData<ProductResponseDto>();
            try
            {
                // Check if product already exists
                var exists = await _productRepo.ProductExistsAsync(createDto.ProductCode,createDto.StoreId);
                if (exists)
                {
                    response.Success = false;
                    response.Message = "Product with same code already exists.";
                    response.ResponsCode = 400;
                    return BadRequest(response);
                }

                // Get StoreId
                var storemaster = await _storeRepo.GetStoreByIdAsync(createDto.StoreId);

                var product = new ProductMaster
                {
                    ProductCode = createDto.ProductCode,
                    BarCode = createDto.BarCode,
                    ProductName = createDto.ProductName,
                    CategoryId = createDto.CategoryId,
                    SubCategoryId = createDto.SubCategoryId == 0 ? null : createDto.SubCategoryId,
                    Quantity = createDto.Quantity,
                    UnitOfMeasure = createDto.UnitOfMeasure,
                    CostPrice = createDto.CostPrice,
                    SellingPrice = createDto.SellingPrice,
                    DiscountPrice = createDto.DiscountPrice,
                    DiscountedPrice = createDto.DiscountPrice,
                    DiscountPercentage = createDto.DiscountPercentage,
                    WholesalePrice = createDto.WholesalePrice,
                    MinimumPrice = createDto.MinimumPrice,
                    MaximumPrice = createDto.MaximumPrice,
                    Description = createDto.Description,
                    IsActive = createDto.IsActive,
                    CreatedUser = createDto.CreatedUser,
                    StoreId = storemaster.Id,
                };

                var createdProduct = await _productRepo.CreateProductAsync(product);

                // Convert to DTO before returning
                var resultDto = new ProductResponseDto
                {
                    Id = createdProduct.Id,
                    ProductCode = createdProduct.ProductCode,
                    BarCode = createdProduct.BarCode,
                    ProductName = createdProduct.ProductName,
                    CategoryId = createdProduct.CategoryId,
                    SubCategoryId = createdProduct.SubCategoryId,
                    Quantity = createdProduct.Quantity,
                    UnitOfMeasure = createdProduct.UnitOfMeasure,
                    CostPrice = createdProduct.CostPrice,
                    SellingPrice = createdProduct.SellingPrice,
                    DiscountPrice = createdProduct.DiscountPrice,
                    DiscountedPrice= createdProduct.DiscountPrice,
                    DiscountPercentage= createdProduct.DiscountPercentage,
                    WholesalePrice = createdProduct.WholesalePrice,
                    MinimumPrice = createdProduct.MinimumPrice,
                    MaximumPrice = createdProduct.MaximumPrice,
                    Description = createdProduct.Description,
                    IsActive = createdProduct.IsActive,
                    StoreId = createdProduct.StoreId,
                    CreatedDate = createdProduct.CreatedDate,
                    CreatedUser = createdProduct.CreatedUser
                };

                response.Success = true;
                response.Message = "Product created successfully.";
                response.Result = resultDto;
                response.ResponsCode = 201;
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.ResponsCode = 400;
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                response.Success = false;
                response.Message = "Failed to create product.";
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
        /// Update existing product.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPut("product/{id}")]
        [ProducesResponseType(typeof(HttpResponseData<UpdateProductResponse>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<UpdateProductResponse>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<UpdateProductResponse>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<UpdateProductResponse>), 500)]
        public async Task<IActionResult> UpdateProduct(long id, [FromBody] UpdateProductDto updateDto)
        {
            var response = new HttpResponseData<UpdateProductResponse>();
            try
            {
                var existingProduct = await _productRepo.GetProductByIdAsync(id,updateDto.StoreId);
                if (existingProduct == null)
                {
                    response.Success = false;
                    response.Message = "Product not found.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                //var product = new ProductMaster
                //{
                //    Id = id,
                //    ProductCode = updateDto.ProductCode,
                //    BarCode = updateDto.BarCode,
                //    ProductName = updateDto.ProductName,
                //    CategoryId = updateDto.CategoryId,
                //    SubCategoryId = updateDto.SubCategoryId == 0 ? null : updateDto.SubCategoryId,
                //    Quantity = updateDto.Quantity,
                //    UnitOfMeasure = updateDto.UnitOfMeasure,
                //    CostPrice = updateDto.CostPrice,
                //    SellingPrice = updateDto.SellingPrice,
                //    DiscountPrice = updateDto.DiscountPrice,
                //    DiscountedPrice = updateDto.DiscountPrice,
                //    DiscountPercentage = updateDto.DiscountPercentage,
                //    WholesalePrice = updateDto.WholesalePrice,
                //    MinimumPrice = updateDto.MinimumPrice,
                //    MaximumPrice = updateDto.MaximumPrice,
                //    Description = updateDto.Description,
                //    IsActive = updateDto.IsActive,
                //    UpdatedUser = updateDto.UpdatedUser,
                //    StoreId = updateDto.StoreId,
                //};

                var updatedProduct = await _productRepo.UpdateProductAsync(id,updateDto);
                

                //minew update
               await UpdateProductInMinew(updatedProduct);

                return Ok(new UpdateProductResponse
                {
                    Success = true,
                    Message = "Product updated successfully.",
                    ResponsCode = 200,
                    ProductId = updatedProduct.Id,
                    ProductName = updatedProduct.ProductName,
                    ProductCode = updatedProduct.ProductCode
                });
            }
            catch (ArgumentException ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.ResponsCode = 400;
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product with ID {ProductId}", id);
                response.Success = false;
                response.Message = "Failed to update product.";
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
        /// Create a product together with all of its ESL device+template/message
        /// assignments in a single database transaction - if any part fails
        /// (bad category, invalid device, etc.) nothing is saved, instead of the
        /// product ending up half-configured.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPost("with-esl")]
        [ProducesResponseType(typeof(HttpResponseData<ProductResponseDto>), 201)]
        [ProducesResponseType(typeof(HttpResponseData<ProductResponseDto>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<ProductResponseDto>), 500)]
        public async Task<IActionResult> CreateProductWithEsl([FromBody] ProductWithEslRequest request)
        {
            return await SaveProductWithEslAsync(null, request);
        }

        /// <summary>
        /// Update a product together with all of its ESL device+template/message
        /// assignments in a single database transaction.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPut("{id}/with-esl")]
        [ProducesResponseType(typeof(HttpResponseData<ProductResponseDto>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<ProductResponseDto>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<ProductResponseDto>), 500)]
        public async Task<IActionResult> UpdateProductWithEsl(long id, [FromBody] ProductWithEslRequest request)
        {
            return await SaveProductWithEslAsync(id, request);
        }

        private async Task<IActionResult> SaveProductWithEslAsync(long? id, ProductWithEslRequest request)
        {
            var response = new HttpResponseData<ProductResponseDto>();
            try
            {
                var product = await _productRepo.SaveProductWithEslAsync(
                    id, request.Product, request.EslAssignments, request.UserId);

                //minew update
                await UpdateProductInMinew(product);

                response.Success = true;
                response.Message = id.HasValue
                    ? "Product and ESL assignments updated successfully."
                    : "Product and ESL assignments created successfully.";
                response.Result = new ProductResponseDto
                {
                    Id = product.Id,
                    ProductCode = product.ProductCode,
                    BarCode = product.BarCode,
                    ProductName = product.ProductName,
                    CategoryId = product.CategoryId,
                    SubCategoryId = product.SubCategoryId,
                    Quantity = product.Quantity,
                    UnitOfMeasure = product.UnitOfMeasure,
                    CostPrice = product.CostPrice,
                    SellingPrice = product.SellingPrice,
                    DiscountPrice = product.DiscountPrice,
                    DiscountedPrice = product.DiscountedPrice,
                    DiscountPercentage = product.DiscountPercentage,
                    WholesalePrice = product.WholesalePrice,
                    MinimumPrice = product.MinimumPrice,
                    MaximumPrice = product.MaximumPrice,
                    Description = product.Description,
                    IsActive = product.IsActive,
                    StoreId = product.StoreId,
                    CreatedDate = product.CreatedDate,
                    CreatedUser = product.CreatedUser,
                };
                response.ResponsCode = id.HasValue ? 200 : 201;
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.ResponsCode = 400;
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving product with ESL assignments (id={ProductId})", id);
                response.Success = false;
                response.Message = "Failed to save product and ESL assignments - no changes were made.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(
                     title: response.Message,
                     detail: ex.Message,
                     statusCode: StatusCodes.Status500InternalServerError
                 );
            }
        }

        #region Excel Import / Export

        private static readonly string[] ImportTemplateHeaders = new[]
        {
            "ProductCode", "ProductName", "BarCode", "CategoryName", "SubCategoryName",
            "UnitOfMeasure", "Quantity", "CostPrice", "SellingPrice", "DiscountPrice",
            "WholesalePrice", "MinimumPrice", "MaximumPrice", "Description", "IsActive"
        };

        /// <summary>
        /// Download a blank Excel template (with an example row and a categories
        /// reference sheet) that shows the exact columns expected by the Import endpoint.
        /// </summary>
        [HttpGet("import-template")]
        public async Task<IActionResult> DownloadImportTemplate([FromQuery] long? storeId)
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var sheet = workbook.Worksheets.Add("Products");

            for (int i = 0; i < ImportTemplateHeaders.Length; i++)
            {
                sheet.Cell(1, i + 1).Value = ImportTemplateHeaders[i];
                sheet.Cell(1, i + 1).Style.Font.Bold = true;
                sheet.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGreen;
            }

            // One example row so users can see the expected format.
            var (categories, _) = await _productRepo.GetActiveCategoriesAsync(1, 1, "", storeId);
            var exampleCategory = categories.FirstOrDefault();
            string exampleCategoryName = exampleCategory?.CategoryName ?? "Beverages";
            string exampleSubCategoryName = "";
            if (exampleCategory != null)
            {
                var subCats = await _productRepo.GetSubCategoryByCategory((int)exampleCategory.Id, storeId);
                exampleSubCategoryName = subCats.FirstOrDefault()?.SubCategoryName ?? "";
            }

            var example = new object[]
            {
                "00099", "Example Product Name", "1234567890123", exampleCategoryName, exampleSubCategoryName,
                "EA", 10, 1.50m, 2.50m, 0m, 0m, 0m, 0m, "Optional description", "TRUE"
            };
            for (int i = 0; i < example.Length; i++)
            {
                sheet.Cell(2, i + 1).Value = example[i].ToString();
                sheet.Cell(2, i + 1).Style.Font.FontColor = ClosedXML.Excel.XLColor.Gray;
                sheet.Cell(2, i + 1).Style.Font.Italic = true;
            }

            sheet.Columns().AdjustToContents();

            // Reference sheet listing valid category/subcategory names for this store,
            // so users don't have to guess spelling.
            var refSheet = workbook.Worksheets.Add("Categories Reference");
            refSheet.Cell(1, 1).Value = "CategoryName";
            refSheet.Cell(1, 2).Value = "SubCategoryName";
            refSheet.Cell(1, 1).Style.Font.Bold = true;
            refSheet.Cell(1, 2).Style.Font.Bold = true;

            var (allCategories, _) = await _productRepo.GetActiveCategoriesAsync(1, 1000, "", storeId);
            int refRow = 2;
            foreach (var category in allCategories)
            {
                var subCats = await _productRepo.GetSubCategoryByCategory((int)category.Id, storeId);
                if (subCats.Any())
                {
                    foreach (var sub in subCats)
                    {
                        refSheet.Cell(refRow, 1).Value = category.CategoryName;
                        refSheet.Cell(refRow, 2).Value = sub.SubCategoryName;
                        refRow++;
                    }
                }
                else
                {
                    refSheet.Cell(refRow, 1).Value = category.CategoryName;
                    refRow++;
                }
            }
            refSheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "product-import-template.xlsx");
        }

        /// <summary>
        /// Export all products (for the given store) to an Excel file, using the
        /// same column layout as the import template so an exported file can be
        /// edited and re-imported.
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> ExportProducts([FromQuery] long? storeId, [FromQuery] string searchTerm = "")
        {
            var (products, _) = await _productRepo.GetAllProductsAsync(1, int.MaxValue, storeId, null, null, searchTerm);

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var sheet = workbook.Worksheets.Add("Products");

            for (int i = 0; i < ImportTemplateHeaders.Length; i++)
            {
                sheet.Cell(1, i + 1).Value = ImportTemplateHeaders[i];
                sheet.Cell(1, i + 1).Style.Font.Bold = true;
                sheet.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGreen;
            }

            int row = 2;
            foreach (var p in products)
            {
                sheet.Cell(row, 1).Value = p.ProductCode;
                sheet.Cell(row, 2).Value = p.ProductName;
                sheet.Cell(row, 3).Value = p.BarCode;
                sheet.Cell(row, 4).Value = p.CategoryName;
                sheet.Cell(row, 5).Value = p.SubCategoryName;
                sheet.Cell(row, 6).Value = p.UnitOfMeasure;
                sheet.Cell(row, 7).Value = p.Quantity;
                sheet.Cell(row, 8).Value = p.CostPrice;
                sheet.Cell(row, 9).Value = p.SellingPrice;
                sheet.Cell(row, 10).Value = p.DiscountPrice;
                sheet.Cell(row, 11).Value = p.WholesalePrice;
                sheet.Cell(row, 12).Value = p.MinimumPrice;
                sheet.Cell(row, 13).Value = p.MaximumPrice;
                sheet.Cell(row, 14).Value = p.Description;
                sheet.Cell(row, 15).Value = p.IsActive ? "TRUE" : "FALSE";
                row++;
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName = $"products-export-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        /// <summary>
        /// Bulk create/update products from an uploaded Excel file matching the
        /// import-template column layout. Matches existing products by ProductCode;
        /// unmatched codes are created. Each row is validated independently so one
        /// bad row doesn't block the rest of the file.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPost("import")]
        [ProducesResponseType(typeof(HttpResponseData<ImportProductsResultDto>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<ImportProductsResultDto>), 400)]
        public async Task<IActionResult> ImportProducts([FromForm] IFormFile file, [FromQuery] long storeId, [FromQuery] int userId = 0)
        {
            var response = new HttpResponseData<ImportProductsResultDto>();
            var result = new ImportProductsResultDto();

            try
            {
                if (file == null || file.Length == 0)
                {
                    response.Success = false;
                    response.Message = "Please upload a file";
                    response.ResponsCode = 400;
                    return BadRequest(response);
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    response.Success = false;
                    response.Message = "Invalid file type. Please upload an Excel (.xlsx, .xls) file.";
                    response.ResponsCode = 400;
                    return BadRequest(response);
                }

                var (categories, _) = await _productRepo.GetActiveCategoriesAsync(1, 1000, "", storeId);
                var categoryByName = categories.ToDictionary(c => c.CategoryName.Trim(), c => c, StringComparer.OrdinalIgnoreCase);

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
                var worksheet = workbook.Worksheets.First();
                var rows = worksheet.RowsUsed().Skip(1); // skip header row

                foreach (var xlRow in rows)
                {
                    result.TotalRows++;
                    int excelRowNumber = xlRow.RowNumber();

                    try
                    {
                        var productCode = xlRow.Cell(1).GetString().Trim();
                        var productName = xlRow.Cell(2).GetString().Trim();
                        var barCode = xlRow.Cell(3).GetString().Trim();
                        var categoryName = xlRow.Cell(4).GetString().Trim();
                        var subCategoryName = xlRow.Cell(5).GetString().Trim();
                        var unitOfMeasure = xlRow.Cell(6).GetString().Trim();
                        var description = xlRow.Cell(14).GetString().Trim();
                        var isActiveText = xlRow.Cell(15).GetString().Trim();

                        if (string.IsNullOrWhiteSpace(productCode) || string.IsNullOrWhiteSpace(productName))
                        {
                            result.Failed++;
                            result.Errors.Add($"Row {excelRowNumber}: ProductCode and ProductName are required.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(categoryName) || !categoryByName.TryGetValue(categoryName, out var category))
                        {
                            result.Failed++;
                            result.Errors.Add($"Row {excelRowNumber}: Category '{categoryName}' was not found for this store.");
                            continue;
                        }

                        long? subCategoryId = null;
                        if (!string.IsNullOrWhiteSpace(subCategoryName))
                        {
                            var subCats = await _productRepo.GetSubCategoryByCategory((int)category.Id, storeId);
                            var subCategory = subCats.FirstOrDefault(s => string.Equals(s.SubCategoryName.Trim(), subCategoryName, StringComparison.OrdinalIgnoreCase));
                            if (subCategory == null)
                            {
                                result.Failed++;
                                result.Errors.Add($"Row {excelRowNumber}: Sub category '{subCategoryName}' was not found under category '{categoryName}'.");
                                continue;
                            }
                            subCategoryId = subCategory.Id;
                        }

                        decimal quantity = xlRow.Cell(7).TryGetValue(out decimal q) ? q : 0;
                        decimal costPrice = xlRow.Cell(8).TryGetValue(out decimal cp) ? cp : 0;
                        decimal sellingPrice = xlRow.Cell(9).TryGetValue(out decimal sp) ? sp : 0;
                        decimal discountPrice = xlRow.Cell(10).TryGetValue(out decimal dp) ? dp : 0;
                        decimal wholesalePrice = xlRow.Cell(11).TryGetValue(out decimal wp) ? wp : 0;
                        decimal minimumPrice = xlRow.Cell(12).TryGetValue(out decimal minp) ? minp : 0;
                        decimal maximumPrice = xlRow.Cell(13).TryGetValue(out decimal maxp) ? maxp : 0;
                        bool isActive = !string.Equals(isActiveText, "FALSE", StringComparison.OrdinalIgnoreCase);

                        if (sellingPrice <= 0)
                        {
                            result.Failed++;
                            result.Errors.Add($"Row {excelRowNumber}: SellingPrice must be greater than 0.");
                            continue;
                        }

                        var existing = await _productRepo.GetProductByCodeAsync(productCode, storeId);

                        if (existing != null)
                        {
                            existing.ProductName = productName;
                            existing.BarCode = barCode;
                            existing.CategoryId = category.Id;
                            existing.SubCategoryId = subCategoryId;
                            existing.UnitOfMeasure = unitOfMeasure;
                            existing.Quantity = quantity;
                            existing.CostPrice = costPrice;
                            existing.SellingPrice = sellingPrice;
                            existing.DiscountPrice = discountPrice;
                            existing.WholesalePrice = wholesalePrice;
                            existing.MinimumPrice = minimumPrice;
                            existing.MaximumPrice = maximumPrice;
                            existing.Description = description;
                            existing.IsActive = isActive;
                            existing.UpdatedUser = userId;

                            await _productRepo.UpdateProductAsync(existing);
                            result.Updated++;
                        }
                        else
                        {
                            var newProduct = new ProductMaster
                            {
                                ProductCode = productCode,
                                ProductName = productName,
                                BarCode = barCode,
                                CategoryId = category.Id,
                                SubCategoryId = subCategoryId,
                                UnitOfMeasure = unitOfMeasure,
                                Quantity = quantity,
                                CostPrice = costPrice,
                                SellingPrice = sellingPrice,
                                DiscountPrice = discountPrice,
                                WholesalePrice = wholesalePrice,
                                MinimumPrice = minimumPrice,
                                MaximumPrice = maximumPrice,
                                Description = description,
                                IsActive = isActive,
                                CreatedUser = userId,
                                StoreId = storeId,
                            };

                            await _productRepo.CreateProductAsync(newProduct);
                            result.Created++;
                        }
                    }
                    catch (Exception rowEx)
                    {
                        result.Failed++;
                        result.Errors.Add($"Row {excelRowNumber}: {rowEx.Message}");
                    }
                }

                response.Success = true;
                response.Result = result;
                response.Message = $"Import finished: {result.Created} created, {result.Updated} updated, {result.Failed} failed.";
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing products from Excel");
                response.Success = false;
                response.Message = "Failed to import products.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return StatusCode(500, response);
            }
        }

        #endregion

        /// <summary>
        /// Update product in Minew ESL system
        /// </summary>
        private async Task UpdateProductInMinew(ProductMaster product)
        {
            try
            {
                // Get the Minew Store ID from StoreMaster
                var storeMaster = await _storeRepo.GetStoreByProductIdAsync(product.Id);

                if (storeMaster == null || string.IsNullOrEmpty(storeMaster.MinewStoreId))
                {
                    _logger.LogWarning("No MinewStoreId found for product {ProductId}, skipping Minew update", product.Id);
                    return;
                }

                // No barcode guard: Minew documents barcode as optional on
                // goods/addToStore, and the required fields on updateToStore are
                // id, storeId and price. Skipping the push for a product without
                // a barcode silently stranded every price edit for such products.

                // Create the update request for Minew
                var updateRequest = new MinewUpdateProductRequest
                {
                    id = product.Id.ToString(),           // Product ID
                    storeId = storeMaster.MinewStoreId,
                    price = product.SellingPrice.ToString("0.00"),
                    barcode = product.BarCode ?? "",
                    p_name = product.ProductName,
                    p_code = product.ProductCode ?? "",
                    discount = product.DiscountPrice.ToString("0.00"),
                    qrcode = "http://minewtag.com",       // Default or generate dynamically
                                                          // Additional fields that might be used in your template
                    specification = "2.9",                // Default or from product
                    unit = "001f",                        // Default unit code
                    memberPrice = "",                     // Empty or calculate if you have member price
                    origin = "",                          // Origin info if available
                    image = "",                           // Image URL if available
                    barcoode = product.BarCode ?? ""      // Note the typo 'barcoode' if required
                };

                // Call Minew API to update product
                var result = await _minewCloudService.UpdateProductInStoreAsync(updateRequest);

                if (result?.code != 200)
                {
                    _logger.LogWarning("Failed to update product {ProductId} in Minew: {Message}",
                        product.Id, result?.msg ?? result?.message ?? "Unknown error");

                    // Update sync status in product
                  //  await UpdateProductSyncStatus(product.Id, false);
                }
                else
                {
                    _logger.LogInformation("Product {ProductId} updated successfully in Minew", product.Id);

                    // Update sync status in product
                   // await UpdateProductSyncStatus(product.Id, true);
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - we don't want Minew failure to break main update
                _logger.LogError(ex, "Error updating product {ProductId} in Minew ESL system", product.Id);
               // await UpdateProductSyncStatus(product.Id, false);
            }
        }

        /// <summary>
        /// Update product sync status
        /// </summary>
        //private async Task UpdateProductSyncStatus(long productId, bool isSynced)
        //{
        //    try
        //    {
        //        var product = await _productRepo.GetProductByIdAsync(productId);
        //        if (product != null)
        //        {
        //            product.IsSyncToCloud = isSynced;
        //            await _productRepo.UpdateProductAsync(product);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error updating sync status for product {ProductId}", productId);
        //    }
        //}

        /// <summary>
        /// Generate QR Code URL for product (optional)
        /// </summary>
        private string GenerateQrCodeUrl(ProductMaster product)
        {
            // Implement based on your requirements
            // Example: return $"{_configuration["BaseUrl"]}/product/{product.Id}";
            return string.Empty;
        }

        /// <summary>
        /// Delete an existing product.
        /// </summary>
        [Authorize(Roles = "Admin,Manager")]
        [HttpDelete("product/{id}")]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 500)]
        public async Task<IActionResult> DeleteProduct(long id, long? storeId)
        {
            var response = new HttpResponseData<bool>();
            try
            {
                var result = await _productRepo.DeleteProductAsync(id,storeId);
                if (!result)
                {
                    response.Success = false;
                    response.Message = "Product not found.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                response.Success = true;
                response.Message = "Product deleted successfully.";
                response.Result = true;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with ID {ProductId}", id);
                response.Success = false;
                response.Message = "Failed to delete product.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Retrieves products by category ID.
        /// </summary>
        [HttpGet("category/{categoryId:int}")]
        [ProducesResponseType(typeof(HttpResponseData<List<ProductViewDto>>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<List<ProductViewDto>>), 500)]
        public async Task<IActionResult> GetByCategoryAsync(int categoryId,long? storeId)
        {
            var response = new HttpResponseData<List<ProductViewDto>>();
            try
            {
                var products = await _productRepo.GetProductsByCategoryAsync(categoryId, storeId);
                response.Success = true;
                response.Message = $"Products for category {categoryId} retrieved successfully.";
                response.Result = products.ToList();
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving products for category {categoryId}");
                response.Success = false;
                response.Message = "Failed to retrieve products by category.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Retrieves a product by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(HttpResponseData<ProductViewDto>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<ProductViewDto>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<ProductViewDto>), 500)]
        public async Task<IActionResult> GetByIdAsync(int id, long? storeId)
        {
            var response = new HttpResponseData<ProductViewDto>();
            try
            {
                var product = await _productRepo.GetProductByIdAsync(id, storeId);
                if (product == null)
                {
                    response.Success = false;
                    response.Message = $"Product with ID {id} not found.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                response.Success = true;
                response.Message = "Product retrieved successfully.";
                response.Result = product;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving product ID {id}");
                response.Success = false;
                response.Message = "Failed to retrieve product.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
       
        ///// <summary>
        ///// Updates device info for a product.
        ///// </summary>
        //[HttpPut("{productId}/user/{userId}/device")]
        //[ProducesResponseType(typeof(HttpResponseData<bool>), 204)]
        //[ProducesResponseType(typeof(HttpResponseData<bool>), 400)]
        //[ProducesResponseType(typeof(HttpResponseData<bool>), 500)]
        //public async Task<IActionResult> UpdateProductDeviceAsync(int productId, int userId, [FromBody] ProductDeviceUpdateDto deviceInfo)
        //{
        //    var response = new HttpResponseData<bool>();
        //    try
        //    {
        //        await _productRepo.UpdateProductDeviceAsync(productId, deviceInfo, userId);
        //        response.Success = true;
        //        response.Message = "Product device updated successfully.";
        //        response.ResponsCode = 204;
        //        response.Result = true;
        //        return NoContent();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Error updating device for product {productId}");
        //        response.Success = false;
        //        response.Message = "Failed to update product device.";
        //        response.Error = ex.Message;
        //        response.ResponsCode = 500;
        //        response.Result = false;
        //        return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        //    }
        //}

        /// <summary>
        /// Synchronizes products in bulk.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPost("sync")]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 500)]
        public async Task<IActionResult> SyncProductsAsync([FromBody] List<ProductMaster> products, long? storeId)
        {
            var response = new HttpResponseData<bool>();
            try
            {
                await _productRepo.SyncProducts(products,storeId);
                response.Success = true;
                response.Message = "Products synchronized successfully.";
                response.ResponsCode = 200;
                response.Result = true;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error synchronizing products");
                response.Success = false;
                response.Message = "Failed to synchronize products.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                response.Result = false;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        #endregion

        #region Product Category Handlers
        /// <summary>
        /// Retrieves all active categories.
        /// </summary>
        [HttpGet("all-categories")]
        [ProducesResponseType(typeof(HttpResponseData<PagedResult<ProductCategory>>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<PagedResult<ProductCategory>>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<PagedResult<ProductCategory>>), 500)]
        public async Task<IActionResult> GetCategoriesAsync([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchTerm = "", [FromQuery] long? storeId = null)
        {
            var response = new HttpResponseData<PagedResult<ProductCategory>>();

            try
            {
                var (products, totalCount) = await _productRepo.GetActiveCategoriesAsync(pageNumber, pageSize, searchTerm,storeId);

                var result = new PagedResult<ProductCategory>
                {
                    Items = products.ToList(),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    SearchTerm = searchTerm
                };

                response.Success = true;
                response.Message = totalCount == 0 && !string.IsNullOrEmpty(searchTerm)
                    ? $"No categories found matching '{searchTerm}'"
                    : "Categories retrieved successfully.";
                response.Result = result;
                response.ResponsCode = totalCount == 0 ? 404 : 200;

                return totalCount == 0 ? NotFound(response) : Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories with pagination");
                response.Success = false;
                response.Message = "Failed to retrieve categories.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Retrieves a category by Id
        /// </summary>
        [HttpGet("category/{id}")]
        [ProducesResponseType(typeof(HttpResponseData<ProductCategory>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<ProductCategory>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<ProductCategory>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<ProductCategory>), 500)]
        public async Task<IActionResult> GetCategory(int id, long? storeId)
        {
            var response = new HttpResponseData<ProductCategory>();
            try
            {
                var category = await _productRepo.GetCategoryByIdAsync(id,storeId);
                if (category == null)
                {
                    response.Success = false;
                    response.Message = "Category not found.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                response.Success = true;
                response.Message = "Category retrieved successfully.";
                response.Result = category;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category with ID {CategoryId}", id);
                response.Success = false;
                response.Message = "Failed to retrieve category.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Create a new category
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPost("category")]
        [ProducesResponseType(typeof(HttpResponseData<ProductCategory>), 201)]
        [ProducesResponseType(typeof(HttpResponseData<ProductCategory>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<ProductCategory>), 500)]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto createDto)
        {
            var response = new HttpResponseData<ProductCategory>();
            try
            {
                // Check if category already exists
                var exists = await _productRepo.CategoryExistsAsync(createDto.CategoryCode, createDto.CategoryName,createDto.StoreId);
                if (exists)
                {
                    response.Success = false;
                    response.Message = "Category with same code or name already exists.";
                    response.ResponsCode = 400;
                    return BadRequest(response);
                }

                var category = new ProductCategory
                {
                    CategoryName = createDto.CategoryName,
                    CategoryCode = createDto.CategoryCode,
                    CategoryDescription = createDto.CategoryDescription,
                    IsActive = createDto.IsActive,
                    CreatedUser = createDto.CreatedUser,
                    StoreId = createDto.StoreId,
                };

                var createdCategory = await _productRepo.CreateCategoryAsync(category);

                response.Success = true;
                response.Message = "Category created successfully.";
                response.Result = createdCategory;
                response.ResponsCode = 201;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                response.Success = false;
                response.Message = "Failed to create category.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Update an existing category
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPut("category/{id}")]
        [ProducesResponseType(typeof(HttpResponseData<ProductCategory>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<ProductCategory>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<ProductCategory>), 500)]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto updateDto)
        {
            var response = new HttpResponseData<ProductCategory>();
            try
            {
                var existingCategory = await _productRepo.GetCategoryByIdAsync(id,updateDto.StoreId);
                if (existingCategory == null)
                {
                    response.Success = false;
                    response.Message = "Category not found.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                var category = new ProductCategory
                {
                    Id = id,
                    CategoryName = updateDto.CategoryName,
                    CategoryCode = updateDto.CategoryCode,
                    CategoryDescription = updateDto.CategoryDescription,
                    IsActive = updateDto.IsActive,
                    UpdatedUser = updateDto.UpdatedUser
                };

                var updatedCategory = await _productRepo.UpdateCategoryAsync(category);

                response.Success = true;
                response.Message = "Category updated successfully.";
                response.Result = updatedCategory;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category with ID {CategoryId}", id);
                response.Success = false;
                response.Message = "Failed to update category.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Delete a category by Id
        /// </summary>
        [Authorize(Roles = "Admin,Manager")]
        [HttpDelete("category/{id}")]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 500)]
        public async Task<IActionResult> DeleteCategory(long id, long? storeId)
        {
            var response = new HttpResponseData<bool>();
            try
            {
                var result = await _productRepo.DeleteCategoryAsync(id,storeId);
                if (!result)
                {
                    response.Success = false;
                    response.Message = "Category not found.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                response.Success = true;
                response.Message = "Category deleted successfully.";
                response.Result = true;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.ResponsCode = 400;
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category with ID {CategoryId}", id);
                response.Success = false;
                response.Message = "Failed to delete category.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        #endregion

        #region Product SubCategory Handlers
        /// <summary>
        /// Retrieves active sub categories with pagination and search.
        /// </summary>
        [HttpGet("active-subcategories")]
        [ProducesResponseType(typeof(HttpResponseData<PagedResult<ProductSubCategory>>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<PagedResult<ProductSubCategory>>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<PagedResult<ProductSubCategory>>), 500)]
        public async Task<IActionResult> GetActiveSubCategoriesAsync([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] int? categoryId = null, [FromQuery] int? storeId = null, [FromQuery] string searchTerm = "")
        {
            var response = new HttpResponseData<PagedResult<ProductSubCategory>>();
            try
            {
                var (categories, totalCount) = await _productRepo.GetActiveSubCategoriesAsync(pageNumber, pageSize, searchTerm, categoryId,storeId);

                var result = new PagedResult<ProductSubCategory>
                {
                    Items = categories.ToList(),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    SearchTerm = searchTerm
                };

                response.Success = true;
                response.Message = totalCount == 0 && !string.IsNullOrEmpty(searchTerm)
                    ? $"No active subcategories found matching '{searchTerm}'"
                    : "Active Subcategories retrieved successfully.";
                response.Result = result;
                response.ResponsCode = 200;

                return Ok(response); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active subcategories with pagination");
                response.Success = false;
                response.Message = "Failed to retrieve active subcategories.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Retrieves a subcategory by Id
        /// </summary>
        [HttpGet("subcategory/{id}")]
        [ProducesResponseType(typeof(HttpResponseData<ProductSubCategory>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<ProductSubCategory>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<ProductSubCategory>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<ProductSubCategory>), 500)]
        public async Task<IActionResult> GetSubCategory(int id, long? storeId)
        {
            var response = new HttpResponseData<SubCategoryResponseDto>();
            try
            {
                var subCategory = await _productRepo.GetSubCategoryByIdAsync(id,storeId);
                if (subCategory == null)
                {
                    response.Success = false;
                    response.Message = "Subcategory not found.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                var result = new SubCategoryResponseDto
                {
                    Id = subCategory.Id,
                    CategoryId = subCategory.CategoryId,
                    //CategoryName = subCategory.Category?.CategoryName,
                    SubCategoryName = subCategory.SubCategoryName,
                    SubCategoryCode = subCategory.SubCategoryCode,
                    SubCategoryDescription = subCategory.SubCategoryDescription,
                    IsActive = subCategory.IsActive,
                    CreatedDate = subCategory.CreatedDate
                };

                response.Success = true;
                response.Message = "Subcategory retrieved successfully.";
                response.Result = result;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subcategory with ID {SubCategoryId}", id);
                response.Success = false;
                response.Message = "Failed to retrieve subcategory.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Create a new subcategory. 
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPost("subcategory")]
        [ProducesResponseType(typeof(HttpResponseData<ProductSubCategory>), 201)]
        [ProducesResponseType(typeof(HttpResponseData<ProductSubCategory>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<ProductSubCategory>), 500)]
        public async Task<IActionResult> CreateSubCategory([FromBody] CreateSubCategoryDto createDto)
        {
            var response = new HttpResponseData<ProductSubCategory>();
            try
            {
                // Check if subcategory already exists
                var exists = await _productRepo.SubCategoryExistsAsync(createDto.SubCategoryCode, createDto.SubCategoryName, createDto.StoreId);
                if (exists)
                {
                    response.Success = false;
                    response.Message = "Subcategory with same code or name already exists.";
                    response.ResponsCode = 400;
                    return BadRequest(response);
                }

                var subCategory = new ProductSubCategory
                {
                    CategoryId = createDto.CategoryId,
                    SubCategoryName = createDto.SubCategoryName,
                    SubCategoryCode = createDto.SubCategoryCode,
                    SubCategoryDescription = createDto.SubCategoryDescription,
                    IsActive = createDto.IsActive,
                    CreatedUser = createDto.CreatedUser,
                    StoreId = createDto.StoreId,
                };

                var createdSubCategory = await _productRepo.CreateSubCategoryAsync(subCategory, createDto.StoreId);

                response.Success = true;
                response.Message = "Subcategory created successfully.";
                response.Result = createdSubCategory;
                response.ResponsCode = 201;
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.ResponsCode = 400;
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subcategory");
                response.Success = false;
                response.Message = "Failed to create subcategory.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
        /// <summary>
        /// Update existing subcategory.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPut("subcategory/{id}")]
        [ProducesResponseType(typeof(HttpResponseData<ProductSubCategory>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<ProductSubCategory>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<ProductSubCategory>), 500)]
        public async Task<IActionResult> UpdateSubCategory(int id, [FromBody] UpdateSubCategoryDto updateDto)
        {
            var response = new HttpResponseData<ProductSubCategory>();
            try
            {
                var existingSubCategory = await _productRepo.GetSubCategoryByIdAsync(id, updateDto.StoreId);
                if (existingSubCategory == null)
                {
                    response.Success = false;
                    response.Message = "Subcategory not found.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                var subCategory = new ProductSubCategory
                {
                    Id = id,
                    CategoryId = updateDto.CategoryId,
                    SubCategoryName = updateDto.SubCategoryName,
                    SubCategoryCode = updateDto.SubCategoryCode,
                    SubCategoryDescription = updateDto.SubCategoryDescription,
                    IsActive = updateDto.IsActive,
                    UpdatedUser = updateDto.UpdatedUser
                };

                var updatedSubCategory = await _productRepo.UpdateSubCategoryAsync(subCategory,updateDto.StoreId);

                response.Success = true;
                response.Message = "Subcategory updated successfully.";
                response.Result = updatedSubCategory;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.ResponsCode = 400;
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subcategory with ID {SubCategoryId}", id);
                response.Success = false;
                response.Message = "Failed to update subcategory.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Delete a subcategory by Id
        /// </summary>
        [Authorize(Roles = "Admin,Manager")]
        [HttpDelete("subcategory/{id}")]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 500)]
        public async Task<IActionResult> DeleteSubCategory(long id, long? storeId)
        {
            var response = new HttpResponseData<bool>();
            try
            {
                var result = await _productRepo.DeleteSubCategoryAsync(id,storeId);
                if (!result)
                {
                    response.Success = false;
                    response.Message = "Subcategory not found.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                response.Success = true;
                response.Message = "Subcategory deleted successfully.";
                response.Result = true;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.ResponsCode = 400;
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subcategory with ID {SubCategoryId}", id);
                response.Success = false;
                response.Message = "Failed to delete subcategory.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Retrieves all subcategories by Category Id
        /// </summary>
        [HttpGet("subcategories/category/{id}")]
        [ProducesResponseType(typeof(HttpResponseData<List<SubCategoryResponseDto>>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<List<SubCategoryResponseDto>>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<List<SubCategoryResponseDto>>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<List<SubCategoryResponseDto>>), 500)]
        public async Task<IActionResult> GetSubCategoriesByCategory(int id, long? storeId)
        {
            var response = new HttpResponseData<List<SubCategoryResponseDto>>();
            try
            {
                var subCategories = await _productRepo.GetSubCategoryByCategory(id, storeId);

                if (subCategories == null || !subCategories.Any())
                {
                    response.Success = false;
                    response.Message = "No subcategories found for this category.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                var result = subCategories.Select(subCategory => new SubCategoryResponseDto
                {
                    Id = subCategory.Id,
                    CategoryId = subCategory.CategoryId,
                    //CategoryName = subCategory.Category?.CategoryName,
                    SubCategoryName = subCategory.SubCategoryName,
                    SubCategoryCode = subCategory.SubCategoryCode,
                    SubCategoryDescription = subCategory.SubCategoryDescription,
                    IsActive = subCategory.IsActive,
                    CreatedDate = subCategory.CreatedDate
                }).ToList();

                response.Success = true;
                response.Message = "Subcategories retrieved successfully.";
                response.Result = result;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subcategories for category ID {CategoryId}", id);
                response.Success = false;
                response.Message = "Failed to retrieve subcategories.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
        #endregion
    }
}
