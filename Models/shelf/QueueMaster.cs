using System;
using System.ComponentModel.DataAnnotations.Schema;
using TERMS_LOYALTY_API.DTOs.shelf;

namespace TERMS_LOYALTY_API.Models.shelf
{
    public class QueueMaster: BaseEntity
    {
        public long? AisleId { get; set; }
        public long? ShelfId { get; set; }
        public long? ProductId { get; set; }
        public long? MessageId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int DisplayOrder { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public long? StatusId { get; set; }

        // New properties for binding operations
        public int? PriorityId { get; set; }

        public string QueueType { get; set; } = "DISPLAY"; // DISPLAY, BIND, SYNC, MAINTENANCE
        public long? DeviceId { get; set; }
        public string TemplateId { get; set; }
        public string LocationType { get; set; } // AISLE, SHELF, PRODUCT
        public long? LocationId { get; set; }
        public string BindingData { get; set; } // JSON string for bind operations
        public string ExecutionResult { get; set; }
        public int RetryCount { get; set; } = 0;
        public int MaxRetries { get; set; } = 3;
        public string ErrorMessage { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public DateTime? LastAttempt { get; set; }
        public bool IsRecurring { get; set; } = false;
        public string RecurrencePattern { get; set; } // DAILY, WEEKLY, MONTHLY, HOURLY
        public long StoreId { get; set; }


        [ForeignKey("StatusId")]
        public virtual Status Status { get; set; }

        [ForeignKey("PriorityId")]
        public virtual PriorityMaster Priority { get; set; }

        [ForeignKey("DeviceId")]
        public virtual DeviceMaster Device { get; set; }

        [ForeignKey("ProductId")]
        public virtual ProductMaster Product { get; set; }
        [ForeignKey("StoreId")]
        public virtual StoreMaster Store { get; set; }

        [ForeignKey("ShelfId")]
        public virtual ShelfMaster Shelf { get; set; }

        [ForeignKey("MessageId")]
        public virtual MessageMaster Message { get; set; }

        [ForeignKey("TemplateId")]
        public virtual MinewTemplates Template { get; set; }
    }
}
