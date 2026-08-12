using System.Collections.Generic;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class ProductSummary
    {
        public class ProductDto
        {
            public long ProductId { get; set; }
            public string ProductName { get; set; }
            public string ProductCode { get; set; }
            public decimal SellingPrice { get; set; }
            public decimal DiscountPrice { get; set; }
        }

        public class ShelfProductGroupDto
        {
            public long ShelfId { get; set; }
            public string ShelfName { get; set; }
            public List<ProductDto> Products { get; set; } = new();
        }

        public class AisleProductResponseDto
        {
            public long AisleId { get; set; }
            public string AisleName { get; set; }
            public List<ProductDto> AisleLevelProducts { get; set; } = new();
            public List<ShelfProductGroupDto> Shelves { get; set; } = new();
        }

        public class ProductSummaryResponseDto
        {
            public int TotalProductCount { get; set; }
            public List<AisleProductResponseDto> Aisles { get; set; } = new();
        }
    }
}
