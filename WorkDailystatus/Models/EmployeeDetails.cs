namespace WorkPulse.Models
{
    public class EmployeeDetails
    {
        public int Id { get; set; }          // Primary key (optional use)
        public int UserId { get; set; }      // Foreign key (login user)

        public string? Name { get; set; }
        public string? EmployeeCode { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public string? Email { get; set; }

        // New fields
        public string Company { get; set; } = string.Empty;

        public string? ProfileImagePath { get; set; }
    }
}