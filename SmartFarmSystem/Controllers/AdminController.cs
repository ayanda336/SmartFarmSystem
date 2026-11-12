using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartFarmSystem.Data;
using SmartFarmSystem.Models;
using SmartFarmSystem.Services;
using SmartFarmSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace SmartFarmSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailSender _emailSender; // add this field

        public AdminController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            INotificationService notificationService,
            IEmailSender emailSender) // add this parameter)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _notificationService = notificationService;
            _emailSender = emailSender;
        }

        // ================= USER LIST =================

        // GET: Admin/Index
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModel = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isLockedOut = await _userManager.IsLockedOutAsync(user);

                userRolesViewModel.Add(new UserRoleViewModel
                {
                    User = user,
                    Roles = roles,
                    IsActive = !isLockedOut
                });
            }

            return View(userRolesViewModel);
        }

        // ================= ROLE MANAGEMENT =================

        // GET: Admin/ManageRoles/5
        // GET: Admin/ManageRoles/5
        public async Task<IActionResult> ManageRoles(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            // EXCLUDE Admin role from the dropdown (only Manager and Worker)
            var allRoles = await _roleManager.Roles
                .Where(r => r.Name != "Admin")
                .ToListAsync();

            var viewModel = new UserRoleViewModel
            {
                User = user,
                Roles = userRoles,
                AllRoles = allRoles,
                SelectedRole = userRoles.FirstOrDefault()
            };

            return View(viewModel);
        }

        // POST: Admin/ManageRoles/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(string id, string? selectedRole)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!string.IsNullOrEmpty(selectedRole))
            {
                await _userManager.AddToRoleAsync(user, selectedRole);
            }

            await LogAction($"Changed role for {user.Email} to '{selectedRole ?? "none"}'");

            return RedirectToAction(nameof(Index));
        }

        // ================= CREATE / DEACTIVATE / DELETE USERS =================

        

// GET: Admin/CreateUser
public IActionResult CreateUser()
    {
        ViewBag.AllRoles = _roleManager.Roles.ToList();
        return View();
    }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string email, string? role)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("", "Email is required.");
                ViewBag.AllRoles = _roleManager.Roles.ToList();
                return View();
            }

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                ModelState.AddModelError("", $"A user with email '{email}' already exists.");
                ViewBag.AllRoles = _roleManager.Roles.ToList();
                return View();
            }

            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var temporaryPassword = GenerateSecurePassword();
            var result = await _userManager.CreateAsync(user, temporaryPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                ViewBag.AllRoles = _roleManager.Roles.ToList();
                return View();
            }

            if (!string.IsNullOrEmpty(role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var setupLink = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code = token, email = user.Email },
                protocol: Request.Scheme);

            await LogAction($"Created user {email} with role '{role ?? "none"}'");

            try
            {
                await _emailSender.SendEmailAsync(
                    user.Email!,
                    "Set up your SmartFarm account",
                    $"<p>An account has been created for you on SmartFarm.</p>" +
                    $"<p><a href='{setupLink}'>Click here to set your password</a> and log in.</p>" +
                    $"<p>This link can only be used once.</p>");

                TempData["Message"] = $"Invitation sent to {user.Email}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                return View("UserCreated", new UserCreatedViewModel
                {
                    Email = user.Email!,
                    SetupLink = setupLink!,
                    EmailFailed = true
                });
            }
        }

        private static string GenerateSecurePassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string symbols = "!@#$%^&*";
            const string all = upper + lower + digits + symbols;

            var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
            var chars = new char[16];
            for (int i = 0; i < 16; i++)
                chars[i] = all[bytes[i] % all.Length];

            chars[0] = upper[System.Security.Cryptography.RandomNumberGenerator.GetInt32(upper.Length)];
            chars[1] = lower[System.Security.Cryptography.RandomNumberGenerator.GetInt32(lower.Length)];
            chars[2] = digits[System.Security.Cryptography.RandomNumberGenerator.GetInt32(digits.Length)];
            chars[3] = symbols[System.Security.Cryptography.RandomNumberGenerator.GetInt32(symbols.Length)];

            return new string(chars);
        }

        // POST: Admin/DeleteUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Unlink any worker record pointing at this user
            var linkedWorker = await _context.Workers.FirstOrDefaultAsync(w => w.UserId == id);
            if (linkedWorker != null)
            {
                linkedWorker.UserId = null;
                _context.Update(linkedWorker);
            }

            // Remove notifications belonging to this user (FK would otherwise block the delete)
            var notifications = await _context.Notifications.Where(n => n.UserId == id).ToListAsync();
            if (notifications.Any())
            {
                _context.Notifications.RemoveRange(notifications);
            }

            await _context.SaveChangesAsync();

            var email = user.Email;
            await _userManager.DeleteAsync(user);
            await LogAction($"Deleted user {email}");

            return RedirectToAction(nameof(Index));
        }
        // ================= PASSWORD RESET =================

        // GET: Admin/ResetPassword/5
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            ViewBag.UserEmail = user.Email;
            return View(model: id);
        }

        // POST: Admin/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                ViewBag.UserEmail = user.Email;
                return View(model: id);
            }

            await LogAction($"Reset password for {user.Email}");
            return RedirectToAction(nameof(Index));
        }

        // ================= WORKER LINKING =================

        // GET: Admin/LinkWorker
        public async Task<IActionResult> LinkWorker()
        {
            var allUsers = await _userManager.Users.ToListAsync();

            var linkedUserIds = await _context.Workers
                .Where(w => w.UserId != null)
                .Select(w => w.UserId)
                .ToListAsync();

            var unlinkedUsers = allUsers
                .Where(u => !linkedUserIds.Contains(u.Id))
                .ToList();

            var unlinkedWorkers = await _context.Workers
                .Where(w => w.UserId == null)
                .ToListAsync();

            ViewBag.Users = unlinkedUsers;
            ViewBag.Workers = unlinkedWorkers;

            return View();
        }

        // POST: Admin/LinkWorker
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkWorker(string userId, int workerId)
        {
            if (string.IsNullOrEmpty(userId) || workerId == 0)
            {
                return BadRequest("Please select both a user and a worker.");
            }

            var worker = await _context.Workers.FindAsync(workerId);
            if (worker == null) return NotFound();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            worker.UserId = userId;
            _context.Update(worker);
            await _context.SaveChangesAsync();

            if (!await _userManager.IsInRoleAsync(user, "Worker"))
            {
                await _userManager.AddToRoleAsync(user, "Worker");
            }

            await LogAction($"Linked user {user.Email} to worker {worker.FullName}");

            return RedirectToAction(nameof(LinkWorker));
        }

        // ================= DASHBOARD =================

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            await _notificationService.CheckAndGenerateAlerts();

            var viewModel = new DashboardViewModel
            {
                TotalProjects = await _context.Projects.CountAsync(),
                TotalWorkers = await _context.Workers.CountAsync(),
                TotalResources = await _context.Resources.CountAsync(),
                TotalTasks = await _context.TaskItems.CountAsync(),
                PendingTasks = await _context.TaskItems.CountAsync(t => t.Status != "Completed"),
                CompletedTasks = await _context.TaskItems.CountAsync(t => t.Status == "Completed"),

                LowStockResources = await _context.Resources
                    .Where(r => r.Quantity <= r.MinimumStock)
                    .ToListAsync(),

                RecentTasks = await _context.TaskItems
                    .Include(t => t.Project)
                    .Include(t => t.Worker)
                    .OrderByDescending(t => t.AssignedDate)
                    .Take(5)
                    .ToListAsync()
            };

            var projectStatuses = await _context.Projects
                .GroupBy(p => p.Status ?? "Unknown")
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            foreach (var item in projectStatuses)
            {
                viewModel.ProjectStatusCounts[item.Status] = item.Count;
            }

            var taskStatuses = await _context.TaskItems
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            foreach (var item in taskStatuses)
            {
                viewModel.TaskStatusCounts[item.Status] = item.Count;
            }

            // Total income / expenses / profit across the WHOLE operation
            ViewBag.GrandTotalSales = await _context.Sales.SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;
            ViewBag.GrandTotalExpenses = await _context.Expenses.SumAsync(e => (decimal?)e.Amount) ?? 0m;
            ViewBag.GrandTotalProfit = (decimal)ViewBag.GrandTotalSales - (decimal)ViewBag.GrandTotalExpenses;

            // Top performing projects (single grouped query instead of per-project subqueries)
            var performanceList = await _context.Projects
                .Select(p => new
                {
                    ProjectName = p.Name,
                    Profit = (_context.Sales.Where(s => s.ProjectId == p.Id).Sum(s => (decimal?)s.TotalAmount) ?? 0m)
                           - (_context.Expenses.Where(e => e.ProjectId == p.Id).Sum(e => (decimal?)e.Amount) ?? 0m)
                })
                .OrderByDescending(p => p.Profit)
                .Take(5)
                .ToListAsync();

            ViewBag.TopProjects = performanceList;

            return View(viewModel);
        }

        // ================= EXPORTS =================

        // GET: Admin/ExportProjects
        public async Task<IActionResult> ExportProjects()
        {
            var projects = await _context.Projects.ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Id,Name,Status");
            foreach (var p in projects)
            {
                sb.AppendLine($"{p.Id},\"{p.Name}\",{p.Status}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"projects_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        // GET: Admin/ExportUsers
        public async Task<IActionResult> ExportUsers()
        {
            var users = await _userManager.Users.ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Id,Email,Roles");
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                sb.AppendLine($"{u.Id},{u.Email},\"{string.Join(";", roles)}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"users_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        // ================= AUDIT LOG =================

        // GET: Admin/AuditLog
        public async Task<IActionResult> AuditLog()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(200)
                .ToListAsync();

            return View(logs);
        }

        private async Task LogAction(string action)
        {
            var adminId = _userManager.GetUserId(User) ?? "unknown";
            _context.AuditLogs.Add(new AuditLog
            {
                AdminId = adminId,
                Action = action,
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }
}