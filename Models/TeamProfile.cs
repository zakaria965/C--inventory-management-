using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    public class TeamProfile
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Gender")]
        public string? Gender { get; set; } // Male, Female, Other

        [Display(Name = "Phone Number")]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Email Address")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(200)]
        public string? EmailAddress { get; set; }

        [Display(Name = "Role in System")]
        [StringLength(100)]
        public string? Role { get; set; } // Frontend, Backend, Design, Testing, Consulting

        [Display(Name = "Short Bio / Description")]
        [StringLength(1000)]
        public string? Bio { get; set; }

        [Display(Name = "Profile Image")]
        [StringLength(500)]
        public string? ProfileImagePath { get; set; }

        [Display(Name = "Status")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Updated Date")]
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
    }
}


