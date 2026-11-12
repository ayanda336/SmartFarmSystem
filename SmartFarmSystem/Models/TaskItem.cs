using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartFarmSystem.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Task title is required")]
        [StringLength(200)]
        [Display(Name = "Task Title")]
        public string Title { get; set; }

        [Display(Name = "Description")]
        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Assigned Date")]
        [DataType(DataType.Date)]
        public DateTime AssignedDate { get; set; } = DateTime.Now;

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending"; // Pending, In Progress, Completed

        // Foreign Key to Project
        [Required]
        [Display(Name = "Project")]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }

        // Foreign Key to Worker
        [Required]
        [Display(Name = "Assigned Worker")]
        public int WorkerId { get; set; }

        [ForeignKey("WorkerId")]
        public virtual Worker? Worker { get; set; }

        // Optional: Link to Resource (if a specific resource is needed for this task)
        [Display(Name = "Required Resource")]
        public int? ResourceId { get; set; }

        [ForeignKey("ResourceId")]
        public virtual Resource? Resource { get; set; }

        [Display(Name = "Quantity Needed")]
        [Range(0, int.MaxValue)]
        public int? QuantityNeeded { get; set; }
    }
}