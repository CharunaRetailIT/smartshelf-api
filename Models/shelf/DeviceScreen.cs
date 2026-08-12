using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models.shelf
{
    [Table("DeviceScreens", Schema = "dbo")]
    public class DeviceScreen : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } // e.g., "2.9" E-Paper Black/White"

        [Required]
        public long ScreenTypeId { get; set; } // ESL, LCD, etc.

        [Required]
        public int Width { get; set; } // pixels

        [Required]
        public int Height { get; set; } // pixels

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Inch { get; set; } // screen size in inches

        public string AspectRatio { get; set; } // e.g., "16:9", "4:3"

        public int? DPI { get; set; } // dots per inch

        public bool IsActive { get; set; } = true;

        [MaxLength(500)]
        public string Description { get; set; }

        // Navigation
        [ForeignKey("ScreenTypeId")]
        public virtual DeviceScreenType ScreenType { get; set; }

        public virtual ICollection<DeviceMaster> Devices { get; set; }
    }
}
