using System;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class QueueItemCreateDto
    {
        public long? AisleId { get; set; }
        public long? ShelfId { get; set; }
        public long? ProductId { get; set; }
        public long MsgId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? DisplayOrder { get; set; }
    }
}
