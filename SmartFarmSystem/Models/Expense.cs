using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartFarmSystem.Models
{
    public class Expense
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Amount (R)")]
        [DataType(DataType.Currency)]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Expense Date")]
        [DataType(DataType.Date)]
        public DateTime ExpenseDate { get; set; } = DateTime.Now;

        [Display(Name = "Category")]
        [StringLength(100)]
        public string? Category { get; set; } // e.g., "Labor", "Fuel", "Seeds", "Equipment", "Other"

        [Display(Name = "Receipt/Reference")]
        [StringLength(200)]
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