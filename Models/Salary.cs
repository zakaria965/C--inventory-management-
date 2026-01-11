using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.Models
{
    public class Salary
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        [Required]
        [Display(Name = "Salary Amount")]
        [Range(0, double.MaxValue, ErrorMessage = "Salary must be 0 or greater")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SalaryAmount { get; set; }

        [Required]
        [Display(Name = "Salary Type")]
        public string SalaryType { get; set; } = "Monthly"; // Monthly, Hourly

        [Display(Name = "Hours Worked")]
        [Range(0, double.MaxValue, ErrorMessage = "Hours must be 0 or greater")]
        public decimal? HoursWorked { get; set; }

        [Required]
        [Display(Name = "Month")]
        public int Month { get; set; }

        [Required]
        [Display(Name = "Year")]
        public int Year { get; set; }

        [Display(Name = "Payment Date")]
        [DataType(DataType.Date)]
        public DateTime? PaymentDate { get; set; }

        [Display(Name = "Is Paid")]
        public bool IsPaid { get; set; } = false;

        [Display(Name = "Advance Amount")]
        [Range(0, double.MaxValue, ErrorMessage = "Advance must be 0 or greater")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AdvanceAmount { get; set; } = 0;

        [Display(Name = "Net Salary")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal NetSalary { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }
    }
}

