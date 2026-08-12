using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models.shelf
{
    public class MinewDevices
    {
        [Key]
        [Column("Id")]
        [Required]
        [MaxLength(100)]
        public string Id { get; set; }

        [Column("MACAddress")]
        [Required]
        [MaxLength(50)]
        public string MACAddress { get; set; }

        [Column("DeviceName")]
        [MaxLength(100)]
        public string DeviceName { get; set; }

        [Column("ScreenSize")]
        [MaxLength(20)]
        public string ScreenSize { get; set; }

        public decimal? ScreenInch { get; set; }
        public int? ScreenWidth { get; set; }
        public int? ScreenHeight { get; set; }

        [MaxLength(20)]
        public string ScreenColor { get; set; }

        [Column("Firmware")]
        [MaxLength(50)]
        public string Firmware { get; set; }

        [Column("Hardware")]
        [MaxLength(50)]
        public string Hardware { get; set; }

        [Column("Status")]
        [MaxLength(20)]
        public string Status { get; set; }

        [Column("Battery")]
        public int? Battery { get; set; }

        [Column("LastSeen")]
        public DateTime? LastSeen { get; set; }

        [Column("StoreId")]
        [MaxLength(100)]
        public string StoreId { get; set; }

        [Column("IsActive")]
        [Required]
        public bool IsActive { get; set; } = true;

        [Column("SyncDate")]
        [Required]
        public DateTime SyncDate { get; set; } = DateTime.UtcNow;

        [Column("CreatedDate")]
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
