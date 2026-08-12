using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models.user
{
    [Table("tbDepartments", Schema = "dbo")]

    public class Department : BaseEntity
    {
        public string Name { get; set; } = "";
        public string Code { get; set; } = ""; // Short code like "IT", "HR", "OPS"
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
