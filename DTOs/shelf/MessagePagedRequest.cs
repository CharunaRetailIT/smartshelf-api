using System;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class MessagePagedRequest :BasePagedRequest
    {
        public string? Title { get; set; }
        public int? ContentType { get; set; }
        public bool? IsActive { get; set; } = true;
        public int? CreatedBy { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public DateTime? UpdatedFrom { get; set; }
        public DateTime? UpdatedTo { get; set; }
        public long? StoreId { get; set; }
    }
}
