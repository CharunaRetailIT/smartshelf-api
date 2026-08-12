namespace TERMS_LOYALTY_API.Models.DTOs.user
{
    public class RegisterDto
    {
        public string EmployeeId { get; set; } = "";  // login ID

        public int? RoleId { get; set; } = 4; // Default role ID for User, can be set to 1 for Admin if needed
        public string Password { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";      
        public int? Department { get; set; } 
        public string? Address1 { get; set; }
        public string? Address2 { get; set; } 
        public string? Address3 { get; set; }
        public string? PasswordHash { get; set; }
    }
}
