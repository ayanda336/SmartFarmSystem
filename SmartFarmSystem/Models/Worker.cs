using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SmartFarmSystem.Models
{
    public class Worker
    {
        public int Id { get; set; }

        // NEW: System-generated Employee ID (e.g., WORK-1001)
        [Display(Name = "Employee ID")]
        public string? EmployeeCode { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Display(Name = "Phone Number")]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Hire Date")]
        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; } = DateTime.Now;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // Optional: Link to the Identity User (so workers can log in)
        [Display(Name = "Associated User")]
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; }
    }
}