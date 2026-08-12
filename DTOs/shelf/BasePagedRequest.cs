using System;
using System.ComponentModel.DataAnnotations;
using TERMS_LOYALTY_API.Models;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class BasePagedRequest
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 10;

        public string SortBy { get; set; }

        public bool SortDescending { get; set; }

        public string SearchTerm { get; set; }
    }

    public class DeviceTemplateComboPagedRequest : BasePagedRequest
    {
        public long? DeviceId { get; set; }
        public string? TemplateId { get; set; }
        public bool? IsDefault { get; set; }
        public bool? IsActive { get; set; } = true;
    }

    public class AssignmentPagedRequest : BasePagedRequest
    {
        public string AssignmentType { get; set; }
        public string LocationType { get; set; }
        public long? LocationId { get; set; }
        public long? DeviceTemplateComboId { get; set; }
        public long? DeviceMessageComboId { get; set; }
        public long? StoreId { get; set; }  
        public bool? IsActive { get; set; } = true;
    }
    public class DeviceScreenPagedRequest : BasePagedRequest
    {
        public long? ScreenTypeId { get; set; }
        public int? MinWidth { get; set; }
        public int? MaxWidth { get; set; }
        public int? MinHeight { get; set; }
        public int? MaxHeight { get; set; }
        public decimal? MinInch { get; set; }
        public decimal? MaxInch { get; set; }
        public bool? IsTouchScreen { get; set; }
        public bool? IsActive { get; set; } = true;
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }
    public class GatewayPagedRequest : PagedRequest
    {
        public long? StoreId { get; set; }
        public bool? IsOnline { get; set; }
        public int? MinBattery { get; set; }
        public int? MaxBattery { get; set; }
        public DateTime? LastSeenFrom { get; set; }
        public DateTime? LastSeenTo { get; set; }
        public string? Status { get; set; }
        public string? GatewayType { get; set; }
    }

    public class QueuePagedRequest : BasePagedRequest
    {
        public long? StoreId { get; set; }
        public string QueueType { get; set; }
        public string Status { get; set; }
        public long? DeviceId { get; set; }
        public string LocationType { get; set; }
        public long? LocationId { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public bool? IsActive { get; set; }
    }
}

