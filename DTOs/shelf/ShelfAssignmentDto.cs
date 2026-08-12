namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class ShelfAssignmentDto
    {
        public string AssignmentType { get; set; } // "TEMPLATE" or "MESSAGE"

        // Template assignment fields
        public long? DeviceTemplateComboId { get; set; }
        public long? DeviceId { get; set; }
        public string DeviceMAC { get; set; }
        public string? TemplateId { get; set; }
        public string TemplateName { get; set; }

        // Message assignment fields
        public long? DeviceMessageComboId { get; set; }
        public long? MessageDeviceId { get; set; }
        public string MessageDeviceMAC { get; set; }
        public long? MessageId { get; set; }
        public string MessageTitle { get; set; }
        public long? MessageContentType { get; set; }

        // Common fields
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }

        //public long? DeviceTemplateComboId { get; set; }
        //public long DeviceId { get; set; }
        //public string DeviceMAC { get; set; }
        //public string TemplatedId { get; set; }
        //public string TemplateName { get; set; }
        //public int DisplayOrder { get; set; }
        //public bool IsActive { get; set; }
    }
}
