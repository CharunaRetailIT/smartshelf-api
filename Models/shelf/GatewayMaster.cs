using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models.shelf
{
    public class GatewayMaster : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string MacAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [ForeignKey("StoreMaster")]
        public long StoreId { get; set; }

        // Minew Cloud ID
        [StringLength(100)]
        public string? MinewGatewayId { get; set; }

        // Gateway Status
        [ForeignKey("Status")]
        public long StatusId { get; set; } = 1; // Default to Active

        [StringLength(50)]
        public string? GatewayType { get; set; } = "Minew";

        [StringLength(50)]
        public string? HardwareVersion { get; set; }

        [StringLength(50)]
        public string? FirmwareVersion { get; set; }

        public int? Battery { get; set; }

        public bool IsOnline { get; set; }

        public DateTime? LastSeen { get; set; }

        public DateTime? LastSyncTime { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual StoreMaster? Store { get; set; }
        public virtual Status? Status { get; set; }
    }
}
