using SmartFarmSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartFarmSystem.Services
{
    public interface INotificationService
    {
        Task CreateNotification(string title, string message, string type, string? userId = null, int? relatedId = null, string? relatedType = null);
        Task<List<Notification>> GetUserNotifications(string userId);
        Task<int> GetUnreadCount(string userId);
        Task MarkAsRead(int notificationId);
        Task MarkAllAsRead(string userId);
        Task CheckAndGenerateAlerts();
    }
}