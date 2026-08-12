namespace TERMS_LOYALTY_API.Models.DTOs.user
{
    public class UpdateUserDto
    {
        public string? EmployeeId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public int? Department { get; set; }
        public string? Address1 { get; set; } = string.Empty;
        public string? Address2 { get; set;} = string.Empty;
        public string? Address3 { get; set;} = string.Empty;

    }
}
