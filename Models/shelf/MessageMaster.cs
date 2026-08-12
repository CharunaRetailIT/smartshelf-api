using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models.shelf
{
    public class MessageMaster : BaseEntity
    {
        public string Title { get; set; }
        public long ContentType { get; set; }
        public string ContentData { get; set; }
        public string FileUrl { get; set; }
        public string FabricJsData { get; set; }
        public int Duration { get; set; } = 5;
        public long? StoreId { get; set; }
        public bool IsActive { get; set; } = true;
        public long? ScreenSizeId { get; set; }

        [ForeignKey(nameof(ContentType))]
        public ContentType ContentTypes { get; set; }
    }
}
