using System.ComponentModel.DataAnnotations;

namespace SmartFarmSystem.Models
{
    public class Resource
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Resource name is required")]
        [StringLength(100)]
        [Display(Name = "Resource Name")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Quantity Available")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative")]
        public int Quantity { get; set; } = 0;

        [Required]
        [StringLength(20)]
        [Display(Name = "Unit")]
        public string Unit { get; set; } = "kg";

        [Display(Name = "Minimum Stock Alert")]
        [Range(0, int.MaxValue)]
        public int MinimumStock { get; set; } = 5;

        [Display(Name = "Unit Price (R)")]
        [Range(0, double.MaxValue)]
        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; } = 0;

        [Display(Name = "Last Restocked")]
        [DataType(DataType.Date)]
        public DateTime? LastRestockedDate { get; set; }

        // NEW: Image path for resource photos
        [Display(Name = "Image")]
        public string? ImagePath { get; set; }
    }
}