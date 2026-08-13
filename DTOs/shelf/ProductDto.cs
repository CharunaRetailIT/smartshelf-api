using System;
using System.Collections.Generic;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class CreateProductDto
    {
        public string ProductCode { get; set; }
        public string BarCode { get; set; }
        public string ProductName { get; set; }
        public long CategoryId { get; set; }
        public long SubCategoryId { get; set; }
        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal DiscountPrice { get; set; }
        public decimal DiscountedPrice { get; set; } = 0;
        public decimal DiscountPercentage { get; set; }
        public decimal WholesalePrice { get; set; }
        public decimal MinimumPrice { get; set; }
        public decimal MaximumPrice { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
        public int CreatedUser { get; set; }
        public long StoreId { get; set; }
    }

    public class UpdateProductDto
    {
        public string ProductCode { get; set; }
        public string BarCode { get; set; }
        public string ProductName { get; set; }
        public long CategoryId { get; set; }
        public long SubCategoryId { get; set; }
        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal DiscountPrice { get; set; }
        public decimal DiscountedPrice { get; set; } = 0;
        public decimal DiscountPercentage { get; set; }
        public decimal WholesalePrice { get; set; }
        public decimal MinimumPrice { get; set; }
        public decimal MaximumPrice { get; set; }
        public string Description { get; set; }

        // Nullable on purpose: a plain bool defaults to false, so any caller that
        // omitted this field silently deactivated the product it was editing.
        // Null now means "leave the current value alone".
        public bool? IsActive { get; set; }

        public int? UpdatedUser { get; set; }
        public long StoreId { get; set; }
    }

    public class ProductResponseDto
    {
        public long Id { get; set; }
        public string ProductCode { get; set; }
        public string BarCode { get; set; }
        public string ProductName { get; set; }
        public long CategoryId { get; set; }
        public string CategoryName { get; set; }
        public long? SubCategoryId { get; set; }
        public string SubCategoryName { get; set; }
        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal DiscountPrice { get; set; }
        public decimal DiscountedPrice { get; set; } = 0;
        public decimal DiscountPercentage { get; set; }
        public decimal WholesalePrice { get; set; }
        public decimal MinimumPrice { get; set; }
        public decimal MaximumPrice { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int CreatedUser { get; set; }
        public long StoreId { get; set; }
    }

    public class ImportProductsResultDto
    {
        public int TotalRows { get; set; }
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Failed { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
