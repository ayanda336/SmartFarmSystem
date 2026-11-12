namespace SmartFarmSystem.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string AdminId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}