namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class BindShelfDataRequest
    {
        public long ComboId { get; set; }
        public long MessageId { get; set; }
        public long ShelfId { get; set; }
        public string? ShelfName { get; set; }
        public string? ShelfCode { get; set; }
        public int? Color { get; set; }
        public int? Total { get; set; }
        public int? Period { get; set; }
        public int? Interval { get; set; }
        public int? Brightness { get; set; }
    }
}
