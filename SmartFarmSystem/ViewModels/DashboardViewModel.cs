using SmartFarmSystem.Models;
using System.Collections.Generic;

namespace SmartFarmSystem.ViewModels
{
    public class DashboardViewModel
    {
        // Summary Counts
        public int TotalProjects { get; set; }
        public int TotalWorkers { get; set; }
        public int TotalResources { get; set; }
        public int TotalTasks { get; set; }
        public int PendingTasks { get; set; }
        public int CompletedTasks { get; set; }

        // Low Stock Resources
        public List<Resource> LowStockResources { get; set; } = new List<Resource>();

        // Recent Tasks
        public List<TaskItem> RecentTasks { get; set; } = new List<TaskItem>();

        // Project Status Breakdown (for Chart)
        public Dictionary<string, int> ProjectStatusCounts { get; set; } = new Dictionary<string, int>();

        // Task Status Breakdown (for Chart)
        public Dictionary<string, int> TaskStatusCounts { get; set; } = new Dictionary<string, int>();
    }
}