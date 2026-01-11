using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "SKU")]
        public string SKU { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Quantity in Stock")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be 0 or greater")]
        public int QuantityInStock { get; set; }

        [Required]
        [Display(Name = "Cost Price")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be 0 or greater")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }

        [Required]
        [Display(Name = "Selling Price")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be 0 or greater")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; }

        [Display(Name = "Unit Price (Legacy)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Display(Name = "Category")]
        public string Category { get; set; } = string.Empty;

        [Display(Name = "Supplier")]
        public string? Supplier { get; set; }

        [Display(Name = "Minimum Stock Level")]
        public int? MinimumStockLevel { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Last Updated")]
        public DateTime? LastUpdated { get; set; }

        // Navigation properties
        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
        public ICollection<Outgoing> Outgoings { get; set; } = new List<Outgoing>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}

