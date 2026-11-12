using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartFarmSystem.Models;

namespace SmartFarmSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<SmartFarmSystem.Models.Project> Projects { get; set; }
        public DbSet<Worker> Workers { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<SmartFarmSystem.Models.Sale> Sales { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<SmartFarmSystem.Models.LeaveRequest> LeaveRequests { get; set; }
        public DbSet<SmartFarmSystem.Models.Message> Messages { get; set; }
    }
}
