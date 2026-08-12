using System.Text.Json.Serialization;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class CustomImageMessageDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("fabric_js_data")]
        public string FabricJsData { get; set; } = string.Empty;

        [JsonPropertyName("image_data")]
        public string ImageData { get; set; } = string.Empty;

        [JsonPropertyName("duration")]
        public int Duration { get; set; } = 5;

        [JsonPropertyName("created_by")]
        public int CreatedBy { get; set; }

        [JsonPropertyName("StoreId")]
        public long? storeId { get; set; }
        public long? screenSizeId { get; set; }
    }
}
