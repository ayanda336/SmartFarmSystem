using System.ComponentModel.DataAnnotations;

namespace SmartFarmSystem.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Project name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        [Display(Name = "Project Name")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "Status")]
        public string? Status { get; set; } = "Planning"; // Default value

        // This will track when the project was created (useful for auditing)
        [Display(Name = "Created On")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}