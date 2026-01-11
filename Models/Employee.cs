using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Full Name")]
        public string FullName => $"{FirstName} {LastName}";

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [Display(Name = "Position")]
        public string? Position { get; set; }

        [Display(Name = "Department")]
        public string? Department { get; set; }

        [Display(Name = "Hire Date")]
        [DataType(DataType.Date)]
        public DateTime? HireDate { get; set; }

        [Display(Name = "Salary")]
        [Range(0, double.MaxValue, ErrorMessage = "Salary must be 0 or greater")]
        public decimal? Salary { get; set; }

        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}


