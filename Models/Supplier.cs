using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Supplier Name")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Supplier Category")]
        public string Category { get; set; } = string.Empty;

        [Display(Name = "Contact Person Name")]
        [StringLength(200)]
        public string? ContactPersonName { get; set; }

        [Display(Name = "Phone Number")]
        [StringLength(50)]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Email Address")]
        [EmailAddress]
        [StringLength(200)]
        public string? EmailAddress { get; set; }

        [Display(Name = "Physical Address")]
        [StringLength(500)]
        public string? PhysicalAddress { get; set; }

        [Display(Name = "Status")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Last Updated")]
        public DateTime? LastUpdated { get; set; }

        // Navigation properties
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}


