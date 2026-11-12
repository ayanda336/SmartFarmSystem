using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartFarmSystem.Models
{
    public class LeaveRequest
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Worker")]
        public int WorkerId { get; set; }

        [ForeignKey("WorkerId")]
        public virtual Worker? Worker { get; set; }

        [Required]
        [Display(Name = "Leave Type")]
        public string LeaveType { get; set; } = "Annual"; // Annual, Sick, Vacation, Personal, Unpaid

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "Total Days")]
        public int TotalDays { get; set; }

        [Display(Name = "Reason")]
        [StringLength(500)]
        public string? Reason { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Cancelled

        [Display(Name = "Approved/Rejected By")]
        public string? ReviewedById { get; set; }

        [ForeignKey("ReviewedById")]
        public virtual IdentityUser? ReviewedBy { get; set; }

        [Display(Name = "Review Comment")]
        [StringLength(500)]
        public string? ReviewComment { get; set; }

        [Display(Name = "Requested On")]
        public DateTime RequestedOn { get; set; } = DateTime.Now;

        [Display(Name = "Reviewed On")]
        public DateTime? ReviewedOn { get; set; }
    }
}