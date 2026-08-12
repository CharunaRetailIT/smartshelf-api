using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TERMS_LOYALTY_API.Models.shelf
{
    public class DisplayType : BaseEntity
    {
        [Required, MaxLength(20)]
        public string Code { get; set; }               // 'PRICE', 'MESSAGE'

        [Required, MaxLength(50)]
        public string Name { get; set; }               // 'Price Display', 'Message Display'

        [MaxLength(200)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        //public virtual ICollection<DeviceAssignment> DeviceAssignments { get; set; }
    }
}
