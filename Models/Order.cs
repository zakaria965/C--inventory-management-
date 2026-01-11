using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Order Number")]
        public string OrderNumber { get; set; } = string.Empty;

        [Display(Name = "Customer Name")]
        public string? CustomerName { get; set; }

        [Display(Name = "Customer Email")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? CustomerEmail { get; set; }

        [Display(Name = "Customer Phone")]
        public string? CustomerPhone { get; set; }

        [Display(Name = "Shipping Address")]
        public string? ShippingAddress { get; set; }

        [Display(Name = "Order Date")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending";

        [Display(Name = "Total Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Payment Date")]
        public DateTime? PaymentDate { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        // Navigation property
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}

