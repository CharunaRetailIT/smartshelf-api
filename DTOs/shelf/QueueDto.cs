using System;
using System.ComponentModel.DataAnnotations;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class QueueDto
    {
        public long Id { get; set; }
        public string QueueType { get; set; }
        public string DeviceName { get; set; }
        public string DeviceType { get; set; }
        public string LocationType { get; set; }
        public string LocationName { get; set; }
        public long? ProductId { get; set; }
        public string ProductName { get; set; }
        public long? ShelfId { get; set; }
        public string ShelfName { get; set; }
        public string TemplateName { get; set; }
        public string MessageTitle { get; set; }

        // Queue windows are stored in UTC: the client posts an ISO string in UTC
        // and QueueProcessorService compares against DateTime.UtcNow. EF returns
        // them with Kind=Unspecified, which serialises with no 'Z', so the
        // browser parsed them as local time and showed a queue 5h30m early.
        // Stamping the Kind makes the JSON explicit so clients convert properly.
        private DateTime _startDate;
        private DateTime? _endDate;

        public DateTime StartDate
        {
            get => DateTime.SpecifyKind(_startDate, DateTimeKind.Utc);
            set => _startDate = value;
        }

        public DateTime? EndDate
        {
            get => _endDate.HasValue
                ? DateTime.SpecifyKind(_endDate.Value, DateTimeKind.Utc)
                : (DateTime?)null;
            set => _endDate = value;
        }
        public string Status { get; set; }
        public string Priority { get; set; }
        public bool IsActive { get; set; }
        public bool IsRecurring { get; set; }
        public string RecurrencePattern { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedDate { get; set; }
        public long StoreId { get; set; }
        public string StoreName { get; set; }
    }

    public class QueueDetailDto : QueueDto
    {
        public string DeviceMac { get; set; }
        public string TemplateId { get; set; }
        public long? MessageId { get; set; }
        public string ContentData { get; set; }
        public string FileUrl { get; set; }
        public int? Duration { get; set; }
        public string BindingData { get; set; }
        public string ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public DateTime? LastAttempt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class CreateQueueRequest
    {
        public int? AssignmentId { get; set; } // From existing assignment
        public long? DeviceId { get; set; } // For direct queue
        public string TemplateId { get; set; } // For Minew
        public long? MessageId { get; set; } // For Standard
        public string LocationType { get; set; } // "PRODUCT" or "SHELF"
        public long LocationId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? PriorityId { get; set; }
        public bool IsRecurring { get; set; }
        public string RecurrencePattern { get; set; } // "DAILY", "WEEKLY", "MONTHLY"
        public int UserId { get; set; }
    }

    public class CreateDirectQueueRequest
    {
        [Required]
        public long DeviceId { get; set; }

        // For Minew devices
        public string TemplateId { get; set; }

        // For Standard devices
        public long? MessageId { get; set; }

        [Required]
        public string LocationType { get; set; } // "PRODUCT" or "SHELF"

        [Required]
        public long LocationId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int? PriorityId { get; set; }

        public bool IsRecurring { get; set; }

        public string RecurrencePattern { get; set; }

        [Required]
        public int UserId { get; set; }

        public string QueueType { get; set; } // "TEMPLATE_QUEUE" or "MESSAGE_QUEUE"
    }

    // CreateQueueFromAssignmentRequest.cs
    public class CreateQueueFromAssignmentRequest
    {
        [Required]
        public long AssignmentId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int? PriorityId { get; set; }

        public int? DisplayOrder { get; set; }

        public bool IsRecurring { get; set; }

        public string RecurrencePattern { get; set; }

        [Required]
        public int UserId { get; set; }
    }

    // UpdateQueueRequest.cs
    public class UpdateQueueRequest
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? PriorityId { get; set; }
        public int? DisplayOrder { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsRecurring { get; set; }
        public string RecurrencePattern { get; set; }
        public int UserId { get; set; }
    }
}
