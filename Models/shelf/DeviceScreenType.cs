using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models.shelf
{
    [Table("DeviceScreenTypes", Schema = "dbo")]
    public class DeviceScreenType : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } // e.g., "ESL Ink", "LCD", "OLED", "AMOLED", "E-Paper"

        [MaxLength(200)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<DeviceScreen> DeviceScreens { get; set; }
    }
}
