using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace TERMS_LOYALTY_API.Models
{
    public class ExternalLoyaltyTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("External_Loyalty_Txn_ID")]  // ✅ Correct column mapping
        public long ExternalLoyaltyTxnID { get; set; }

        [Required]
        [Column("TransactionType")]
        public int TransactionType { get; set; }

        [Required]
        [Column("LoyaltyCustomerID")]
        public long LoyaltyCustomerID { get; set; }

        [Required]
        [Column("TransactionDate")]
        public DateTime TransactionDate { get; set; }

        [Required]
        [Column("LocationID")]
        public int LocationID { get; set; }

        [Column("UnitNo")]
        public int? UnitNo { get; set; }

        [Column("UserName")]
        [StringLength(200)]
        public string? UserName { get; set; }


        
        [Column("MobileNo")]
        [StringLength(20)]
        public string? MobileNo { get; set; }


        [Column("UserID")]
        public long? UserID { get; set; }

        [Column("OpeningPointBalance")]
        public decimal? OpeningPointBalance { get; set; }

        [Column("EarningPoint")]
        public decimal? EarningPoint { get; set; }

        [Column("RedeemPoint")]
        public decimal? RedeemPoint { get; set; }

        [Column("ClosingPointBalance")]
        public decimal? ClosingPointBalance { get; set; }

        [Column("CreatedDate")]
        public DateTime? CreatedDate { get; set; }
    }
}
