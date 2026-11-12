using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartFarmSystem.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Title")]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Message")]
        [StringLength(1000)]
        public string Message { get; set; }

        [Display(Name = "Is Read")]
        public bool IsRead { get; set; } = false;

        [Display(Name = "Created On")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Notification Type")]
        public string? Type { get; set; } // "LowStock", "TaskOverdue", "TaskAssigned", "General"

        // Link to a specific user (optional - null means broadcast to all)
        [Display(Name = "Target User")]
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; }

        // Link to related entity (optional)
        public int? RelatedId { get; set; }
        public string? RelatedType { get; set; } // "Task", "Resource", "Project"
    }
}