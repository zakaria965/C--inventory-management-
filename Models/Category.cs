using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;
    }
}
