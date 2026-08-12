using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TERMS_LOYALTY_API.Models.shelf
{
    public class PriorityMaster
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string PriorityCode { get; set; }

        [Required, MaxLength(50)]
        public string PriorityName { get; set; }

        [MaxLength(200)]
        public string Description { get; set; }

        public int Value { get; set; }  // Lower = higher priority

        [MaxLength(20)]
        public string ColorCode { get; set; }

        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 1;
        public virtual ICollection<QueueMaster> QueueItems { get; set; }

    }
}
