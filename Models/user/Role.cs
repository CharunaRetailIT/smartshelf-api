using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models.user
{
    [Table("tbRoles", Schema = "dbo")]

    public class Role : BaseEntity
    {
        public string Name { get; set; } = ""; // e.g., "User", "Manager", "Admin"
        public bool IsActive { get; set; } = true;
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
