namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class UpdateCustomImageMessageDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FabricJsData { get; set; } = string.Empty;
        public string ImageData { get; set; } = string.Empty; // base64 string
        public int Duration { get; set; }
        public bool IsActive { get; set; }
        public int UpdatedBy { get; set; } = 0;
        public long? storeId { get; set; }
        public long? ScreenSizeId { get; set; }
    }
}

