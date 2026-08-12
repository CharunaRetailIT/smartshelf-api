using System.Collections.Generic;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class AssignmentIdsResponse
    {
        public List<long> AisleIds { get; set; } = new();
        public List<long> ShelfIds { get; set; } = new();
    }
}
