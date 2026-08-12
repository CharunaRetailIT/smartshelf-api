using System.ComponentModel.DataAnnotations;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class DeviceAssignmentRequest
    {
        [Required]
        public string AssignmentType { get; set; } // "TEMPLATE" or "MESSAGE"

        // For existing combos - provide either combo ID
        public long? DeviceTemplateComboId { get; set; }
        public long? DeviceMessageComboId { get; set; }

        // For creating new combos - provide device and template/message details
        public long? DeviceId { get; set; }
        public string TemplateId { get; set; } // For TEMPLATE assignments
        public long? MessageId { get; set; } // For MESSAGE assignments

        public int DisplayOrder { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
    }
}
