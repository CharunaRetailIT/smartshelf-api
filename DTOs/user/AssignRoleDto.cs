namespace TERMS_LOYALTY_API.Models.DTOs.user
{
    public class AssignRoleDto
    {
        public int? UserId { get; set; } 
        public int Role { get; set; }  // "User", "Manager", "Admin"
    }
}
