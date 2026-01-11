using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    public class Message
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Content")]
        public string? Content { get; set; }

        [Display(Name = "Customer Name")]
        public string? CustomerName { get; set; }

        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Email Address")]
        [EmailAddress]
        public string? EmailAddress { get; set; }

        [Display(Name = "Subject")]
        public string? Subject { get; set; }

        [Display(Name = "Message Type")]
        public string MessageType { get; set; } = "Internal"; // Internal, Contact

        [Display(Name = "Is Read")]
        public bool IsRead { get; set; } = false;

        [Display(Name = "Is Important")]
        public bool IsImportant { get; set; } = false;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Read Date")]
        public DateTime? ReadDate { get; set; }
    }
}

