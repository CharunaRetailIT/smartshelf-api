using System;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class AssignmentViewModel
    {
        // Core assignment properties
        public long Id { get; set; }
        public string AssignmentType { get; set; } // "TEMPLATE" or "MESSAGE"
        public long? DeviceTemplateComboId { get; set; }
        public long? DeviceMessageComboId { get; set; }

        // Device info
        public long DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string DeviceMac { get; set; }
        public int? DeviceHeight { get; set; }
        public int? DeviceWidth { get; set; }
        public int? Battery { get; set; }

        // Template info (for TEMPLATE type)
        public string TemplateId { get; set; }
        public string TemplateName { get; set; }

        // Message info (for MESSAGE type)
        public long? MessageId { get; set; }
        public string MessageTitle { get; set; }
        public string MessageContent { get; set; }

        // Location info
        public string LocationType { get; set; }
        public long LocationId { get; set; }
        public string LocationName { get; set; }

        // Store info
        public long StoreId { get; set; }
        public string StoreName { get; set; }

        // Display properties
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }

        // Audit trail
        public DateTime CreatedDate { get; set; }
        public int CreatedUserId { get; set; }
        public string CreatedUser { get; set; }

        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedUserId { get; set; }
        public string UpdatedUser { get; set; }


        // Helper properties for UI
        public bool IsTemplateAssignment => AssignmentType == "TEMPLATE";
        public bool IsMessageAssignment => AssignmentType == "MESSAGE";

        public string ComboName => IsTemplateAssignment ? TemplateName : MessageTitle;
        public string DeviceDisplay => $"{DeviceName} ({DeviceMac})";

        public string GetComboTypeDescription()
        {
            return AssignmentType switch
            {
                "TEMPLATE" => $"Template: {TemplateName}",
                "MESSAGE" => $"Message: {MessageTitle}",
                _ => "Unknown Assignment"
            };
        }
    }
}
