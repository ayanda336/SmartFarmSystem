using Microsoft.EntityFrameworkCore;
using SmartFarmSystem.Data;
using SmartFarmSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartFarmSystem.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateNotification(string title, string message, string type, string? userId = null, int? relatedId = null, string? relatedType = null)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                UserId = userId,
                RelatedId = relatedId,
                RelatedType = relatedType,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUserNotifications(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId || n.UserId == null)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCount(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => (n.UserId == userId || n.UserId == null) && !n.IsRead);
        }

        public async Task MarkAsRead(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsRead(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => (n.UserId == userId || n.UserId == null) && !n.IsRead)
                .ToListAsync();

            foreach (var n in notifications)
            {
                n.IsRead = true;
            }
            await _context.SaveChangesAsync();
        }

        public async Task CheckAndGenerateAlerts()
        {
            // 1. Check Low Stock Resources
            var lowStockResources = await _context.Resources
                .Where(r => r.Quantity <= r.MinimumStock)
                .ToListAsync();

            foreach (var resource in lowStockResources)
            {
                await CreateNotification(
                    $"⚠️ Low Stock Alert: {resource.Name}",
                    $"{resource.Name} is running low! Current stock: {resource.Quantity} {resource.Unit}. Minimum required: {resource.MinimumStock} {resource.Unit}.",
                    "LowStock",
                    null,
                    resource.Id,
                    "Resource"
                );
            }

            // 2. Check Overdue Tasks
            var overdueTasks = await _context.TaskItems
                .Include(t => t.Worker)
                .Include(t => t.Project)
                .Where(t => t.DueDate.HasValue && t.DueDate < DateTime.Now && t.Status != "Completed")
                .ToListAsync();

            foreach (var task in overdueTasks)
            {
                var worker = await _context.Workers.FindAsync(task.WorkerId);
                if (worker != null && !string.IsNullOrEmpty(worker.UserId))
                {
                    await CreateNotification(
                        $"⏰ Task Overdue: {task.Title}",
                        $"Task '{task.Title}' for project '{task.Project?.Name}' was due on {task.DueDate?.ToShortDateString()} and is still not completed.",
                        "TaskOverdue",
                        worker.UserId,
                        task.Id,
                        "Task"
                    );
                }
            }
        }
    }
}