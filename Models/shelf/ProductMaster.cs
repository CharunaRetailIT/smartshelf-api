using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models.shelf
{
    [Table("ProductMaster", Schema = "dbo")]
    public class ProductMaster : BaseEntity
    {
        // Identification
        [StringLength(50)]
        public string ProductCode { get; set; }
        [StringLength(50)]
        public string? BarCode { get; set; }
        [StringLength(50)]
        public string ProductName { get; set; }

        // Category hierarchy
        public long CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public ProductCategory? Category { get; set; }

        public long? SubCategoryId { get; set; }

        [ForeignKey(nameof(SubCategoryId))]
        public ProductSubCategory? SubCategory { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }
        [StringLength(20)]
        public string UnitOfMeasure { get; set; }

        // Pricing (main for ESL display)
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountPrice { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountedPrice { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal WholesalePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinimumPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MaximumPrice { get; set; }

        // Other info useful for ESL
        [StringLength(75)]
        public string? Description { get; set; }

        // Status
        [DefaultValue(true)]
        public bool IsActive { get; set; } = true;
        [StringLength(50)]
        public bool IsSyncToCloud { get; set; }
        // Store relationship
        public long StoreId { get; set; }

        [ForeignKey(nameof(StoreId))]
        public virtual StoreMaster? Store { get; set; }
    }
}
