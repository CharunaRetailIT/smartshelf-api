using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class StoreDto
    {
        public long? Id { get; set; }

        [Required(ErrorMessage = "Store name is required")]
        [StringLength(200, ErrorMessage = "Store name cannot exceed 200 characters")]
        public string StoreName { get; set; }

        // Minew rejects a blank number or address on store/add (code 54029), and
        // every store is published to Minew, so both are mandatory up front.
        // It also rejects a non-numeric code (code 54030: 门店编号只能数字),
        // so reject that here rather than after the store is already saved.
        [Required(ErrorMessage = "Store code is required")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Store code must contain digits only")]
        [StringLength(50, ErrorMessage = "Store code cannot exceed 50 characters")]
        public string StoreCode { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; }

        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
        public string Phone { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; }

        [StringLength(100, ErrorMessage = "Contact person cannot exceed 100 characters")]
        public string ContactPerson { get; set; }

        // Note: there is no local-vs-minew choice. Every store is created
        // locally and published to Minew, so StoreType is set by the server.

        public string MinewStoreId { get; set; }
        public string MinewTemplateId { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsSynced { get; set; } = false;

        public int? CreatedUser { get; set; }
    }

    public class StoreSyncRequestDto
    {
        // Sync is push-only: stores are created locally and pushed up to Minew.
        // Pulling stores down from Minew is intentionally not supported.
        public bool SyncToCloud { get; set; } = true;
        public List<long> StoreIds { get; set; } = new List<long>();
    }

    public class StoreSyncResultDto
    {
        public int SyncedCount { get; set; }
        public int TotalSynced { get; set; }
        public int FailedCount { get; set; }
        public List<string> Messages { get; set; } = new List<string>();
        public List<StoreSyncDetailDto> Details { get; set; } = new List<StoreSyncDetailDto>();
    }

    public class StoreSyncDetailDto
    {
        public long StoreId { get; set; }
        public string StoreName { get; set; }
        public string Operation { get; set; } // created, updated, skipped, failed
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class StoreFilterDto
    {
        public string SearchTerm { get; set; }
        public string StoreType { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsSynced { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "CreatedDate";
        public string SortDirection { get; set; } = "desc";
    }

    [Keyless]
    public class StoreDetailsRow
    {
        public int Id { get; set; }
        public string StoreName { get; set; }
        public string StoreCode { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string ContactPerson { get; set; }
        public string StoreType { get; set; }
        public string MinewStoreId { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public bool IsActive { get; set; }
        public bool IsSynced { get; set; }
        public DateTime? LastSyncDate { get; set; }
        public string SyncStatus { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int DeviceCount { get; set; }
    }
}

