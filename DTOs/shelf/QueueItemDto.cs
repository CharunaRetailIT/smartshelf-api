using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class QueueItemDto
    {
        public long Id { get; set; }

        [Required]
        public string QueueType { get; set; } // BIND, SYNC, MAINTENANCE

        public long? AisleId { get; set; }
        public long? ShelfId { get; set; }
        public long? ProductId { get; set; }
        public long? DeviceId { get; set; }
        public long? MessageId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }
        public long? StatusId { get; set; }
        public int? PriorityId { get; set; }

        // New fields for binding
        public string LocationType { get; set; } // Shelf, Product
        public long? LocationId { get; set; }
        public string TemplateId { get; set; }
        public string BindingData { get; set; } // JSON string
        public string ExecutionResult { get; set; }
        public int RetryCount { get; set; } = 0;
        public int MaxRetries { get; set; } = 3;
        public string ErrorMessage { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public DateTime? LastAttempt { get; set; }
        public bool IsRecurring { get; set; }
        public string RecurrencePattern { get; set; }

        // Audit fields
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int CreatedUser { get; set; }
        public int? UpdatedUser { get; set; }
    }

    // DTOs for creating queue items
    public class CreateQueueItemDto
    {
        [Required]
        public string QueueType { get; set; }

        public long? DeviceId { get; set; }
        public string TemplateId { get; set; }
        public long? ComboId { get; set; }

        // Location info
        public string LocationType { get; set; } // Shelf, Product
        public long? LocationId { get; set; }

        // Product info for binding
        public long? ProductId { get; set; }
        public long? MessageId { get; set; }
        public long? ShelfId { get; set; }

        // Schedule info
        public DateTime? ScheduledTime { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Recurrence
        public bool IsRecurring { get; set; }
        public string RecurrencePattern { get; set; }

        // Priority
        public int? PriorityId { get; set; }
        public int? MaxRetries { get; set; } = 3;

        // Additional data
        public Dictionary<string, string> AdditionalData { get; set; }
    }

    public class QueueExecutionLogDto
    {
        public long Id { get; set; }
        public long QueueMasterId { get; set; }
        public DateTime ExecutionTime { get; set; }
        public string Status { get; set; }
        public string ResultMessage { get; set; }
        public int? DurationMs { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

