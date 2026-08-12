using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models.shelf
{
    [Table("StoreMaster", Schema = "dbo")]
    public class StoreMaster : BaseEntity
    {
        // Common fields
        [Required]
        [StringLength(200)]
        public string StoreName { get; set; }

        [StringLength(50)]
        public string StoreCode { get; set; }

        [StringLength(500)]
        public string Address { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(100)]
        public string ContactPerson { get; set; }

        // Store type: "minew" or "local"
        [StringLength(20)]
        public string StoreType { get; set; } = "local";

        // For Minew stores only
        [StringLength(100)]
        public string MinewStoreId { get; set; }

        [StringLength(100)]
        public string LegacyStoreId { get; set; } = null;  

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // Status
        public bool IsActive { get; set; } = true;

        public bool IsSynced { get; set; } = false;

        // Sync information
        public DateTime? LastSyncDate { get; set; }

        public string SyncStatus { get; set; } = "pending"; // pending, success, failed
                                                            //public string StoreId { get; set; }
                                                            //public bool IsActive { get; set; } = true;
        public virtual ICollection<ProductMaster> Products { get; set; }
        public virtual ICollection<DeviceMaster> Devices { get; set; }


    }
}
