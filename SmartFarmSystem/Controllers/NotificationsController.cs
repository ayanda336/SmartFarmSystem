using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartFarmSystem.Services;
using System.Threading.Tasks;

namespace SmartFarmSystem.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<IdentityUser> _userManager;

        public NotificationsController(INotificationService notificationService, UserManager<IdentityUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var notifications = await _notificationService.GetUserNotifications(user.Id);
            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsRead(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            await _notificationService.MarkAllAsRead(user.Id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> GenerateAlerts()
        {
            await _notificationService.CheckAndGenerateAlerts();
            return RedirectToAction(nameof(Index));
        }
    }
}