using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.Models
{
    public class Outgoing
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Product")]
        public int ProductId { get; set; }

        [Required]
        [Display(Name = "Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        [Display(Name = "Outgoing Price")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be 0 or greater")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? OutgoingPrice { get; set; }

        [Display(Name = "Total Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalAmount { get; set; }

        [Display(Name = "Customer/Recipient")]
        public string? Recipient { get; set; }

        [Display(Name = "Outgoing Date")]
        [DataType(DataType.Date)]
        public DateTime OutgoingDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Reason")]
        public string Reason { get; set; } = "Sale"; // Sale, Damage, Transfer, Return

        [Display(Name = "Order")]
        public int? OrderId { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }
    }
}

