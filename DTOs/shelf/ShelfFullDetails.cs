using System;
using System.Collections.Generic;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class ShelfFullDetails
    {
            public long Id { get; set; }
            public long? AisleId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
            public string Coordinates { get; set; } = string.Empty;
            public string IPAddress { get; set; } = string.Empty;
            public string DeviceName { get; set; } = string.Empty;
            public string MACAddress { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public int? CreatedUser { get; set; }
            public DateTime? CreatedDate { get; set; }
            public int? UpdatedUser { get; set; }
            public DateTime? UpdatedDate { get; set; }

            // Aisle information
            public AisleBasicInfoDto? Aisle { get; set; }

            // Device assignments
            public List<ShelfAssignmentDto> Assignments { get; set; } = new();
        
    }
    public class AisleBasicInfoDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
