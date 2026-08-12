using System.ComponentModel.DataAnnotations;

namespace TERMS_LOYALTY_API.Models.shelf
{
    // Lookup table for ESL vendor/brand (e.g. "Minew", "Standard").
    // Code matches the free-text DeviceMaster.DeviceType value for that brand.
    public class EslBrandMaster : BaseEntity
    {
        [Required, MaxLength(50)]
        public string Code { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
