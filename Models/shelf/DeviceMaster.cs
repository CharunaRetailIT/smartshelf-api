using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models.shelf
{
    public class DeviceMaster : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string MACAddress { get; set; } = string.Empty;

        [MaxLength(50)]
        public string IPAddress { get; set; } = string.Empty;

        [MaxLength(50)]
        public string NetworkName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public long StatusId { get; set; } = 0;

        // Foreign key to Status table
        [ForeignKey("StatusId")]
        public virtual Status Status { get; set; }

        // Device type (e.g., "Minew", "Standard", "Other")
        [MaxLength(50)]
        public string DeviceType { get; set; } = "Minew";

        // Store reference
        public long StoreId { get; set; } = 0;

        // Minew-specific fields (optional)
        public string MinewDeviceId { get; set; } = string.Empty;// Maps to Minew cloud ID 

        public long? ScreenId { get; set; }


        [MaxLength(20)]
        public string ScreenColor { get; set; } = string.Empty;

        // Technical details
        [MaxLength(50)]
        public string Firmware { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Hardware { get; set; } = string.Empty;

        // Battery and status
        public int? Battery { get; set; } = 0;
        public DateTime? LastSeen { get; set; }
        public DateTime? LastSyncTime { get; set; }

        // Flags
        public bool IsActive { get; set; } = true;
        public bool IsOnline { get; set; } = true;


        [ForeignKey("StoreId")]
        public virtual StoreMaster Store { get; set; }
        public virtual ICollection<DeviceTemplateCombos> DeviceTemplateCombos { get; set; }
        [ForeignKey("ScreenId")]
        public virtual DeviceScreen DeviceScreen { get; set; }


    }
}
