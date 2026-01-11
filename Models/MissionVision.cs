using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    public class MissionVision
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Mission statement is required")]
        [Display(Name = "Mission Statement")]
        public string MissionStatement { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vision statement is required")]
        [Display(Name = "Vision Statement")]
        public string VisionStatement { get; set; } = string.Empty;

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}


