using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models.shelf
{
    /// <summary>
    /// One row per queue execution attempt. The table already existed but was
    /// never mapped, so nothing was ever written to it - a queue's history was
    /// limited to the single LastAttempt/StatusId on QueueMaster, overwritten on
    /// every run.
    /// </summary>
    [Table("QueueExecutionLog")]
    public class QueueExecutionLog
    {
        [Key]
        public long Id { get; set; }

        /// <summary>QueueMaster.Id this attempt belongs to.</summary>
        public int QueueEntryId { get; set; }

        /// <summary>What was pushed: see QueueTargetType.</summary>
        public int QueueTargetTypeId { get; set; }

        /// <summary>Id of the template or message that was pushed.</summary>
        public int TargetId { get; set; }

        public DateTime DisplayStartTime { get; set; }
        public DateTime? DisplayEndTime { get; set; }

        /// <summary>How long the attempt took, in milliseconds.</summary>
        public int? ActualDuration { get; set; }

        /// <summary>Mirrors QueueStatus - Completed or Failed.</summary>
        public int StatusId { get; set; }

        public DateTime CreatedDate { get; set; }
    }

    /// <summary>What a queue execution pushed to the label.</summary>
    public enum QueueTargetType
    {
        Template = 1,
        Message = 2,
    }
}
