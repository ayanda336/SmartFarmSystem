using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartFarmSystem.Data;
using SmartFarmSystem.Models;
using SmartFarmSystem.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartFarmSystem.Controllers
{
    [Authorize(Roles = "Worker")]
    public class WorkerDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly INotificationService _notificationService;

        public WorkerDashboardController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: WorkerDashboard
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var worker = await _context.Workers
                .FirstOrDefaultAsync(w => w.UserId == currentUser.Id);

            if (worker == null)
            {
                ViewBag.Message = "You are not linked to any worker profile. Please contact your manager.";
                return View(new List<TaskItem>());
            }

            var tasks = await _context.TaskItems
                .Include(t => t.Project)
                .Include(t => t.Worker)
                .Include(t => t.Resource)
                .Where(t => t.WorkerId == worker.Id)
                .OrderByDescending(t => t.AssignedDate)
                .ToListAsync();

            ViewBag.WorkerName = worker.FullName;
            ViewBag.EmployeeCode = worker.EmployeeCode;
            ViewBag.WorkerId = worker.Id;

            return View(tasks);
        }

        // POST: WorkerDashboard/MarkComplete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkComplete(int id)
        {
            var task = await _context.TaskItems
                .Include(t => t.Project)
                .Include(t => t.Worker)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var worker = await _context.Workers.FirstOrDefaultAsync(w => w.UserId == currentUser.Id);

            if (task.WorkerId != worker.Id)
            {
                return Forbid();
            }

            task.Status = "Completed";
            _context.Update(task);
            await _context.SaveChangesAsync();

            // ===== AUTO-NOTIFICATION: Notify managers that task is complete =====
            var adminAndManagerUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var managerUsers = await _userManager.GetUsersInRoleAsync("Manager");
            var allManagers = adminAndManagerUsers.Concat(managerUsers).Distinct();

            foreach (var manager in allManagers)
            {
                await _notificationService.CreateNotification(
                    $"✅ Task Completed: {task.Title}",
                    $"Worker '{worker.FullName}' has completed task '{task.Title}' for project '{task.Project?.Name}'.",
                    "TaskCompleted",
                    manager.Id,
                    task.Id,
                    "Task"
                );
            }

            return RedirectToAction(nameof(Index));
        }
    }
}