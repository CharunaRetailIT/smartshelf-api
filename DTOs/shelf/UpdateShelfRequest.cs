using System.Collections.Generic;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class UpdateShelfRequest
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public string Coordinates { get; set; }
        public string Description { get; set; }
        public bool? IsActive { get; set; }
        public long? storeId { get; set; }
        public List<int> ProductIds { get; set; }
        public int ModifiedUser { get; set; } = 0;
    }
}
