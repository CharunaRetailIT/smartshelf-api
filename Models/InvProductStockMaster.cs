using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models
{
    public class InvProductStockMaster : BaseEntity
    {
        public long InvProductStockMasterID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [Required]
        public int LocationID { get; set; }

        [Required]
        public int CostCentreID { get; set; }

        [Required]
        public long ProductID { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Stock { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinimumPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ReOrderLevel { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ReOrderQuantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ReOrderPeriod { get; set; }

        [Required]
        public bool IsDelete { get; set; }

        //[Required]
        //public int GroupOfCompanyID { get; set; }

        //[StringLength(50)]
        //public string? CreatedUser { get; set; }

        //[Required]
        //public DateTime CreatedDate { get; set; }

        //[StringLength(50)]
        //public string? ModifiedUser { get; set; }

        //[Required]
        //public DateTime ModifiedDate { get; set; }

        //[Required]
        //public int DataTransfer { get; set; }

        [Required]
        [StringLength(25)]
        public string StockCode { get; set; }

        [StringLength(20)]
        public string? ProductCode { get; set; }

        [StringLength(200)]
        public string? ProductName { get; set; }

        [StringLength(40)]
        public string? Barcode { get; set; }

        [StringLength(30)]
        public string? RefNo1 { get; set; }

        [StringLength(30)]
        public string? RefNo2 { get; set; }

        [Required]
        public int ExtendedID { get; set; }

        [StringLength(30)]
        public string? ExtendedName { get; set; }

        [StringLength(5)]
        public string? PLUCode { get; set; }

        [StringLength(30)]
        public string? PLUName { get; set; }

        [StringLength(30)]
        public string? ISBN { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal WeightPerunit { get; set; }

        [Required]
        public int UomId { get; set; }

        [StringLength(10)]
        public string? Unit { get; set; }

        [Required]
        public int SupplierID { get; set; }

        [StringLength(20)]
        public string? SupplierCode { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MaxPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AvgCost { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal WholeSalePrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal FixedGP { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal GP { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal OpenBal { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal InitSIH { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal InitCost { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AdjQty { get; set; }

        [Required]
        public bool IsWarranty { get; set; }

        [Required]
        public bool IsDamage { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public bool IsBundle { get; set; }

        [Required]
        public bool IsInitialize { get; set; }

        [Required]
        public bool Ispacksize { get; set; }

        [Required]
        public bool Iscommission { get; set; }

        [Required]
        public bool Isdecimal { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal corporatePrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal corporateFixDisAmt { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal corporateFixDisPrec { get; set; }

        [StringLength(20)]
        public string? ManualBatchCode { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ForigenPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal WholesaleMinimumQuantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Margin { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal WholesaleMargin { get; set; }

        [Required]
        public bool IsCountable { get; set; }

        [Required]
        public bool IsEnableMinusStock { get; set; }

        [Required]
        [StringLength(5)]
        public string SubLevelCode { get; set; }

        [Required]
        [StringLength(30)]
        public string SubLevelName { get; set; }

    }
}
