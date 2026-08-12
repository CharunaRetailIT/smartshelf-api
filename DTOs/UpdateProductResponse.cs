namespace TERMS_LOYALTY_API.DTOs
{
    public class UpdateProductResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; }
        public int ResponsCode { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
    }
}
