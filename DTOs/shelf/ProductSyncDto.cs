namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class ProductSyncDto
    {
        public string id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public string quantity { get; set; } = "0"; 
        public string specification { get; set; } = "";
        public string supplier { get; set; } = "";
    }
}
