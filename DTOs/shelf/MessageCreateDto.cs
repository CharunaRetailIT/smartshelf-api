using System.Text.Json.Serialization;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class MessageCreateDto
    {
        public string Title { get; set; }

        public string ContentData { get; set; }
        public int? Duration { get; set; }

        public int CreatedBy { get; set; }

        [JsonPropertyName("StoreId")]
        public long StoreId { get; set; }

        public long ScreenSizeId { get; set; }
    }
    public class MessageUpdateDto
    {
        public string Title { get; set; }

        public string ContentData { get; set; }
        public int? Duration { get; set; }

        public int UpdatedUser { get; set; }
        public bool IsActive { get; set; }

        public long StoreId { get; set; }
        public long ScreenSizeId { get; set; }

    }
}
