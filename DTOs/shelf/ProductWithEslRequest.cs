using System.Collections.Generic;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class ProductWithEslRequest
    {
        public ProductEslData Product { get; set; }
        public List<EslAssignmentIntent> EslAssignments { get; set; } = new();
        public int UserId { get; set; }

        /// <summary>Push the binding to the vendor cloud so the physical label
        /// actually starts showing the product. Default on - set false to record
        /// the assignment locally only.</summary>
        public bool BindToEsl { get; set; } = true;
    }

    public class ProductEslData
    {
        public string ProductCode { get; set; }
        public string BarCode { get; set; }
        public string ProductName { get; set; }
        public long CategoryId { get; set; }
        public long? SubCategoryId { get; set; }
        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal DiscountPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal WholesalePrice { get; set; }
        public decimal MinimumPrice { get; set; }
        public decimal MaximumPrice { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
        public long StoreId { get; set; }
    }

    // One row = one device. TemplateId/MessageId set means "bind this"; leaving
    // one null while its *AssignmentId is set means "the previously-saved
    // binding of that kind was removed". IsDeleted means the whole row was removed.
    public class EslAssignmentIntent
    {
        public long DeviceId { get; set; }
        public string TemplateId { get; set; }
        public long? MessageId { get; set; }
        public long? DeviceTemplateComboId { get; set; }
        public long? DeviceMessageComboId { get; set; }
        public long? TemplateAssignmentId { get; set; }
        public long? MessageAssignmentId { get; set; }
        public int DisplayOrder { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
    }
}
