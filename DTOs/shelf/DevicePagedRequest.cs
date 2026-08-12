using System;
using TERMS_LOYALTY_API.Models;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class DevicePagedRequest : PagedRequest
    {
        public long? StoreId { get; set; }
        public long? BrandId { get; set; }
        public bool? IsOnline { get; set; }
        public int? MinBattery { get; set; }
        public int? MaxBattery { get; set; }
        public DateTime? LastSeenFrom { get; set; }
        public DateTime? LastSeenTo { get; set; }
        public string? Status { get; set; }
    }
}
