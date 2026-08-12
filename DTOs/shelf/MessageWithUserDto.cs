using System;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class MessageWithUserDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public long ContentType { get; set; }
        public string ContentTypeName { get; set; }
        public string? ContentData { get; set; }
        public string? FabricJsData { get; set; }
        public string? FileUrl { get; set; }
        public int Duration { get; set; }
        public long? ScreenSizeId { get; set; }
        public long? StoreId { get; set; }
        public bool IsActive { get; set; } = true;
        public int CreatedUser { get; set; }
        public string CreatedByName { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
