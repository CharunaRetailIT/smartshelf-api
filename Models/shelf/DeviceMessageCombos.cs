using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models.shelf
{
    [Table("DeviceMessageCombos", Schema = "dbo")]

    public class DeviceMessageCombos : BaseEntity
    {
        [Required]
        public long DeviceId { get; set; }

        [Required]
        public long MessageId { get; set; }
        public long StoreId { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("DeviceId")]
        public virtual DeviceMaster Device { get; set; }

        [ForeignKey("MessageId")]
        public virtual MessageMaster Message { get; set; }


        [ForeignKey("StoreId")]
        public virtual StoreMaster Store { get; set; }
    }
}
