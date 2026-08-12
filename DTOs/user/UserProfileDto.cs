namespace TERMS_LOYALTY_API.DTOs.user
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public int DepartmentId { get; set; }
        public string Department { get; set; }
        public int RoleId { get; set; }
        public string Role { get; set; }
        public string ProfileImageUrl { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
    }
    public class UpdateProfileDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Address3 { get; set; }
    }
}
