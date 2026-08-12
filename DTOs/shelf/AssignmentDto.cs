using System;
using System.ComponentModel.DataAnnotations;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class AssignmentDto
    {
        public long Id { get; set; }
        public string AssignmentType { get; set; } // "TEMPLATE" or "MESSAGE"
        public long? DeviceTemplateComboId { get; set; }
        public long? DeviceMessageComboId { get; set; }
        public dynamic Combo { get; set; } // Can be DeviceTemplateComboDto or DeviceMessageComboDto
        public string LocationType { get; set; }
        public long LocationId { get; set; }
        public string? LocationName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedUser { get; set; }
        public class CreateAssignmentRequest
        {
            [Required]
            public string AssignmentType { get; set; } // "TEMPLATE" or "MESSAGE"

            public long? DeviceTemplateComboId { get; set; }
            public long? DeviceMessageComboId { get; set; }

            [Required]
            public string LocationType { get; set; }

            [Required]
            [Range(1, long.MaxValue)]
            public long LocationId { get; set; }

            [Required]
            public int UserId { get; set; }

            public long StoreId { get; set; }
        }

        public class UpdateAssignmentOrderRequest
        {
            public int NewPosition { get; set; }
            public int UserId { get; set; }
        }
    }
}
