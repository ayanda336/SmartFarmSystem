using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartFarmSystem.Models
{
    public class Sale
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200)]
        [Display(Name = "Product/Produce")]
        public string ProductName { get; set; }

        [Required]
        [Display(Name = "Quantity Sold")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }

        [Required]
        [Display(Name = "Unit")]
        [StringLength(20)]
        public string Unit { get; set; } = "kg";

        [Required]
        [Display(Name = "Unit Price (R)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Total Amount (R)")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        [Required]
        [Display(Name = "Sale Date")]
        [DataType(DataType.Date)]
        public DateTime SaleDate { get; set; } = DateTime.Now;

        [Display(Name = "Buyer Name")]
        [StringLength(200)]
        public string? BuyerName { get; set; }

        [Display(Name = "Buyer Contact")]
        [StringLength(100)]
        public string? BuyerContact { get; set; }

        [Display(Name = "Reference/Invoice")]
        [StringLength(100)]
        public string? Reference { get; set; }

        // Foreign Key to Project
        [Required]
        [Display(Name = "Project")]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }

        [Display(Name = "Created On")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}