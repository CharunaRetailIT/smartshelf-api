using System.Collections.Generic;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class CreateAisleRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string Coordinates { get; set; }
        public long StoreId { get; set; }
        public bool IsActive { get; set; } = true;
        public int createdUser { get; set; }
        public List<CreateShelfRequest> Shelves { get; set; } = new();

    }
}
