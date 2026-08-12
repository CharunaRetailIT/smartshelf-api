
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models.user
{
    [Table("tbUsers", Schema = "dbo")]

    public class User : BaseEntity
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string UserName { get; set; } = ""; // Used for login, can be EmployeeId or Username
        public int? RoleId { get; set; } // Foreign key for Role
        public string Email { get; set; } = "";
        public int? DepartmentId { get; set; }
        public string? ProfileImagePath { get; set; }

        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Address3 { get; set; }
        public string EmployeeId { get; set; } = ""; // used as login
        public string PasswordHash { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public bool IsNew { get; set; }


        public virtual Role Role { get; set; } = null!;
        public virtual Department Department { get; set; } = null!;
    }
}
