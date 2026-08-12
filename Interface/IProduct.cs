using DocumentFormat.OpenXml.Bibliography;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.DTOs;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Models.shelf;

namespace TERMS_LOYALTY_API.Interface
{
    public interface IProduct
    {
        // Product Operations
        Task<(IEnumerable<ProductViewDto> Products, int TotalCount)> GetAllProductsAsync(int pageNumber = 1, int pageSize = 10, long? storeId = null, long? categoryId = null, long? subcategoryId = null, string searchTerm = "");
        Task<ProductMaster> CreateProductAsync(ProductMaster product);
        Task<ProductMaster> UpdateProductAsync(ProductMaster product);
        Task<ProductMaster> UpdateProductAsync(long id, UpdateProductDto dto);
        Task<ProductMaster> SaveProductWithEslAsync(long? productId, ProductEslData productData, List<EslAssignmentIntent> assignments, int userId);
        Task<bool> DeleteProductAsync(long id, long? storeId);
        Task<bool> ProductExistsAsync(string productCode, long? storeId);
        Task<ProductMaster> GetProductByCodeAsync(string productCode, long? storeId);
        Task<IEnumerable<ProductViewDto>> GetProductsByCategoryAsync(long categoryId, long? storeId);
        Task<ProductViewDto> GetProductByIdAsync(long productId, long? storeId);
        //Task UpdateProductDeviceAsync(long productId, ProductDeviceUpdateDto deviceInfo, int userId);
        Task SyncProducts(List<ProductMaster> products, long? storeId);
        Task<string> BuildUnsyncedProductsJsonAsync(string storeId, string? opcode);
        Task<IEnumerable<ProductMaster>> GetUnsyncedProductList(long? storeId);


        // Category Operations
        Task<(IEnumerable<ProductCategory> Categories, int TotalCount)> GetActiveCategoriesAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", long? storeId = null);
        Task<ProductCategory> GetCategoryByIdAsync(int id, long? storeId);
        Task<ProductCategory> CreateCategoryAsync(ProductCategory category);
        Task<ProductCategory> UpdateCategoryAsync(ProductCategory category);
        Task<bool> DeleteCategoryAsync(long id, long? storeId);
        Task<bool> CategoryExistsAsync(string categoryCode, string categoryName, long? storeId);

        // SubCategory Operations
        Task<(IEnumerable<ProductSubCategory> SubCategories, int TotalCount)> GetActiveSubCategoriesAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", long? categoryId = null, long? storeId = null);
        Task<ProductSubCategory> GetSubCategoryByIdAsync(int id, long? storeId);
        Task<ProductSubCategory> CreateSubCategoryAsync(ProductSubCategory subCategory, long? storeId);
        Task<ProductSubCategory> UpdateSubCategoryAsync(ProductSubCategory subCategory, long? storeId);
        Task<bool> DeleteSubCategoryAsync(long id, long? storeId);
        Task<bool> SubCategoryExistsAsync(string subCategoryCode, string subCategoryName, long? storeId);
        Task <List<ProductSubCategory>> GetSubCategoryByCategory(int id,long? storeId);
    }
}
