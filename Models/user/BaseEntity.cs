using System;

namespace TERMS_LOYALTY_API.Models.user
{
    public class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        //public string CreatedUser { get; set; }
        //public string? UpdatedUser { get; set; }
    }
}
