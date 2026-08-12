using System;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class MessageWithUserPaged
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public long ContentType { get; set; }
        public string ContentTypeName { get; set; } = string.Empty;
        public string? ContentData { get; set; }
        public string? FabricJsData { get; set; }
        public string? FileUrl { get; set; }
        public int Duration { get; set; } = 5;
        public bool IsActive { get; set; } = true;
        public int CreatedUser { get; set; }
        public string CreatedByName { get; set; } = "Unknown";
        public DateTime CreatedDate { get; set; }
        public int? UpdatedUser { get; set; }
        public string? UpdatedByName { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public long? ScreenSizeId { get; set; }
    }
}
