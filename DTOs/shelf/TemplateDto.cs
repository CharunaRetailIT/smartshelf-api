using System;
using System.Collections.Generic;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class TemplateDto
    {
        public string Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? ScreenInch { get; set; }
        public int? ScreenWidth { get; set; }
        public int? ScreenHeight { get; set; }
        public string Color { get; set; } = string.Empty;
        public int? Orientation { get; set; }
        public string PreviewImage { get; set; } = string.Empty;
        public long? StoreId { get; set; }
        public bool IsActive { get; set; }
        public DateTime SyncDate { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class TemplatePreviewRequest
    {
        public string TemplateId { get; set; }
        public bool isBound { get; set; }
        public string? Mac { get; set; }
        public string? StoreId { get; set; }
    }

    public class TestBindRequest
    {
        public long ComboId { get; set; }
        public Dictionary<string, string> TestData { get; set; }
    }
}
