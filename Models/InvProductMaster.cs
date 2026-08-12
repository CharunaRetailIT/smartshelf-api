using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models
{
    public class InvProductMaster : BaseEntity
    {

        //[Key]
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long InvProductMasterID { get; set; }

        [Required]
        [StringLength(25)]
        public string ProductCode { get; set; }

        [StringLength(40)]
        public string? BarCode { get; set; }

        [StringLength(25)]
        public string? BarCode2 { get; set; }

        [StringLength(25)]
        public string? ReferenceCode1 { get; set; }

        [StringLength(25)]
        public string? ReferenceCode2 { get; set; }

        [StringLength(25)]
        public string? ReferenceCode3 { get; set; }

        [Required]
        [StringLength(150)]
        public string ProductName { get; set; }

        [Required]
        [StringLength(150)]
        public string NameOnInvoice { get; set; }

        public long DepartmentID { get; set; }

        public long CategoryID { get; set; }

        public long SubCategoryID { get; set; }

        public long SubCategory2ID { get; set; }

        public int InvProductTypeID { get; set; }

        public int InvKitchenBarID { get; set; }

        public long SupplierID { get; set; }

        public long UnitOfMeasureID { get; set; }

        [Required]
        [StringLength(25)]
        public string PackSize { get; set; }

        public byte[]? ProductImage { get; set; }

        [StringLength(50)]
        public string? CostingMethod { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal OrderPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AverageCost { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal WholesalePrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinimumPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal FixedDiscount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MaximumDiscount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MaximumPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal FixedDiscountPercentage { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MaximumDiscountPercentage { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ReOrderLevel { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ReOrderQty { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ReOrderPeriod { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public bool IsBatch { get; set; }

        [Required]
        public bool IsPromotion { get; set; }

        [Required]
        public bool IsBundle { get; set; }

        [Required]
        public bool IsFreeIssue { get; set; }

        [Required]
        public bool IsDrayage { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DrayagePercentage { get; set; }

        [Required]
        public bool IsExpiry { get; set; }

        [Required]
        public bool IsConsignment { get; set; }

        [Required]
        public bool IsCountable { get; set; }

        [Required]
        public bool IsDCS { get; set; }

        public long DcsID { get; set; }

        [Required]
        public bool IsTax { get; set; }

        [Required]
        public bool IsSerial { get; set; }

        [Required]
        public bool IsNonExchangeable { get; set; }

        [Required]
        public bool IsDelete { get; set; }

        public long PackSizeUnitOfMeasureID { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Margin { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal WholesaleMargin { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal FixedGP { get; set; }

        public long PurchaseLedgerID { get; set; }

        public long SalesLedgerID { get; set; }

        public long OtherPurchaseLedgerID { get; set; }

        public long OtherSalesLedgerID { get; set; }

        [StringLength(150)]
        public string? Remark { get; set; }

        //public int GroupOfCompanyID { get; set; }

        //[StringLength(50)]
        //public string? CreatedUser { get; set; }

        //[Required]
        //public DateTime CreatedDate { get; set; }

        //[StringLength(50)]
        //public string? ModifiedUser { get; set; }

        //[Required]
        //public DateTime ModifiedDate { get; set; }

        //public int DataTransfer { get; set; }

        [Required]
        public bool IsWeighted { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal WeightPerUnit { get; set; }

        public int SeqNo { get; set; }

        [Required]
        public bool IsTaxIncludePrice { get; set; }

        [StringLength(25)]
        public string? ReferenceCode4 { get; set; }

        [StringLength(25)]
        public string? ReferenceCode5 { get; set; }

        [Required]
        public bool Ispacksize { get; set; }

        [Required]
        public bool Iscommission { get; set; }

        [Required]
        public bool Isdecimal { get; set; }

        [StringLength(25)]
        public string? ReferenceCode6 { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PackPrice { get; set; }

        [Required]
        public bool UnderCost { get; set; }

        [Required]
        [StringLength(150)]
        public string NameInSinhala { get; set; }

        [Required]
        public bool IsSerialItem { get; set; }

        [Required]
        public bool IsLoyalty { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal FixedDiscountPercentageCredit { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal FixedDiscountAmountCredit { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal corporatePrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal corporateFixDisAmt { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal corporateFixDisPrec { get; set; }

        [Required]
        public bool IsWholesale { get; set; }

        public int PrinterType { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CommisionRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ThresholdPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal LowestPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DisplayPrice { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; }

        [Required]
        [StringLength(100)]
        public string URL { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal NoOfPieces { get; set; }

        [Required]
        public bool IsLot { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ForigenPrice { get; set; }

        public int BestBeforeDays { get; set; }

        [Required]
        [StringLength(25)]
        public string InterUnitSize { get; set; }

        [Required]
        public bool IsProductionItem { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CommisionAmount { get; set; }

        [Required]
        public bool IsRefundable { get; set; }

        [Required]
        [StringLength(150)]
        public string Model { get; set; }

        [Required]
        [StringLength(100)]
        public string QRCode { get; set; }

        public string? ImageDesc { get; set; }

        public bool? IsRawMaterial { get; set; }

        [Required]
        public bool IsAllowdecimalqty { get; set; }

        [Required]
        public bool Isdisplayshoppingcart { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal LoyaltyPoints { get; set; }

        [StringLength(100)]
        public string? SupplyCountry { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TNT { get; set; }

        [Required]
        public bool AllowDiscount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ProductTaxTotal { get; set; }

        [Required]
        public bool IsWarranty { get; set; }

        public int WarrantyID { get; set; }

        [Required]
        public bool IsEnableMinusStock { get; set; }

        [StringLength(50)]
        public string? WarrantyType { get; set; }

        public int WarrantyPeriod { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountPrice { get; set; }

        [Required]
        [StringLength(100)]
        public string SAP_MatNumber { get; set; }

        [Required]
        public bool IsSubItem { get; set; }

        [Required]
        [StringLength(250)]
        public string ParentProductCode { get; set; }

        [Required]
        public bool IsStockReduceMainItem { get; set; }

    }
}
