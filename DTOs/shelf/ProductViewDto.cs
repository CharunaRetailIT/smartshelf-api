using System.ComponentModel.DataAnnotations.Schema;
using TERMS_LOYALTY_API.Models.shelf;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class ProductViewDto
    {
        public long Id { get; set; }
        public string ProductCode { get; set; }
        public string? BarCode { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public string? UnitOfMeasure { get; set; }
        public string CategoryName{ get; set; }
        public long CategoryId { get; set; }
        public string? SubCategoryName { get; set; }
        public long? SubCategoryId { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal DiscountPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal WholesalePrice { get; set; }
        public decimal MinimumPrice { get; set; }
        public decimal MaximumPrice { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSyncToCloud { get; set; }
        public long StoreId { get; set; }
    }
}
