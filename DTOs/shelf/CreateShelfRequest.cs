using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class CreateShelfRequest
    {
        [Required]
        public string Name { get; set; }
        public long? AisleId { get; set; }
        public string Location { get; set; }
        public string Coordinates { get; set; }
        public long? storeId { get; set; }
        public string Description { get; set; }

        public int CreatedUser { get; set; }
        public bool IsActive { get; set; } = true;

        // Optional: if you want to accept product IDs during creation
        public List<int> ProductIds { get; set; } = new List<int>();
        // Device assignments for the shelf
        public List<DeviceAssignmentRequest> DeviceAssignments { get; set; } = new List<DeviceAssignmentRequest>();
    }
}
