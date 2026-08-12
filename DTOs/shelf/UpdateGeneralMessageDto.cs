namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class UpdateGeneralMessageDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ContentData { get; set; } = string.Empty;
        public int Duration { get; set; }
        public bool IsActive { get; set; }
        public int UpdatedBy { get; set; } = 0; 
        public long? StoreId { get; set; }
        public long? ScreenSizeId { get; set; }
    }
}
