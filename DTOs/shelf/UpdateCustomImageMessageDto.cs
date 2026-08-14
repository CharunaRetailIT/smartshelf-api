namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class UpdateCustomImageMessageDto
    {
        public long Id { get; set; }

        // Null means "not sent, keep the stored value". These used to default to
        // ""/0/false, so a payload shaped like the API document's own demo blanked
        // the title and deactivated the message it was meant to only re-image.
        public string Title { get; set; }
        public string FabricJsData { get; set; }
        public string ImageData { get; set; } = string.Empty; // base64 string
        public int? Duration { get; set; }
        public bool? IsActive { get; set; }
        public int UpdatedBy { get; set; } = 0;
        public long? storeId { get; set; }
        public long? ScreenSizeId { get; set; }
    }
}

