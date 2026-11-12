using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartFarmSystem.Data;
using SmartFarmSystem.Models;
using SmartFarmSystem.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartFarmSystem.Controllers
{
    [Authorize] // All authenticated users can access basic actions
    public class LeaveRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly INotificationService _notificationService;

        public LeaveRequestsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: LeaveRequests
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            IQueryable<LeaveRequest> query = _context.LeaveRequests
                .Include(l => l.Worker)
                .Include(l => l.ReviewedBy);

            if (!isAdminOrManager)
            {
                // Workers see only their own requests
                var worker = await _context.Workers.FirstOrDefaultAsync(w => w.UserId == user.Id);
                if (worker != null)
                {
                    query = query.Where(l => l.WorkerId == worker.Id);
                }
                else
                {
                    return View(new List<LeaveRequest>());
                }
            }

            return View(await query.OrderByDescending(l => l.RequestedOn).ToListAsync());
        }

        // GET: LeaveRequests/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var worker = await _context.Workers
                .FirstOrDefaultAsync(w => w.UserId == user.Id);

            if (worker == null && !User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                ModelState.AddModelError("", "You are not linked to a worker profile.");
                return View(new LeaveRequest());
            }

            if (worker != null)
            {
                ViewBag.WorkerName = worker.FullName;
                ViewBag.WorkerId = worker.Id;
                var model = new LeaveRequest { WorkerId = worker.Id };
                return View(model);
            }

            // Admin/Manager creating for another worker (show dropdown)
            ViewBag.Workers = new SelectList(_context.Workers, "Id", "FullName");
            return View(new LeaveRequest());
        }

        // POST: LeaveRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("WorkerId,LeaveType,StartDate,EndDate,Reason")] LeaveRequest leaveRequest)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Auto-detect worker profile if user is a worker
            var worker = await _context.Workers
                .FirstOrDefaultAsync(w => w.UserId == user.Id);

            if (worker != null)
            {
                leaveRequest.WorkerId = worker.Id;
            }
            else if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                ModelState.AddModelError("", "You are not linked to a worker profile.");
                return View(leaveRequest);
            }

            // Ensure WorkerId is provided
            if (leaveRequest.WorkerId == 0)
            {
                ModelState.AddModelError("WorkerId", "Please select a worker.");
                ViewBag.Workers = new SelectList(_context.Workers, "Id", "FullName");
                return View(leaveRequest);
            }

            // Calculate total days
            leaveRequest.TotalDays = (int)(leaveRequest.EndDate - leaveRequest.StartDate).TotalDays + 1;
            leaveRequest.Status = "Pending";
            leaveRequest.RequestedOn = DateTime.Now;

            if (ModelState.IsValid)
            {
                _context.Add(leaveRequest);
                await _context.SaveChangesAsync();

                // Get worker name for notifications
                var workerName = worker?.FullName ?? "A worker";

                // Notify all Admins and Managers
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                var managerUsers = await _userManager.GetUsersInRoleAsync("Manager");
                var allManagers = adminUsers.Concat(managerUsers).Distinct();

                foreach (var manager in allManagers)
                {
                    await _notificationService.CreateNotification(
                        $"📋 New Leave Request from {workerName}",
                        $"{workerName} has requested {leaveRequest.LeaveType} leave from {leaveRequest.StartDate.ToShortDateString()} to {leaveRequest.EndDate.ToShortDateString()}.",
                        "LeaveRequest",
                        manager.Id,
                        leaveRequest.Id,
                        "LeaveRequest"
                    );
                }

                // Notify the worker (if linked)
                if (worker != null && !string.IsNullOrEmpty(worker.UserId))
                {
                    await _notificationService.CreateNotification(
                        "✅ Leave Request Submitted",
                        $"Your {leaveRequest.LeaveType} leave request from {leaveRequest.StartDate.ToShortDateString()} to {leaveRequest.EndDate.ToShortDateString()} has been submitted and is pending approval.",
                        "LeaveRequest",
                        worker.UserId,
                        leaveRequest.Id,
                        "LeaveRequest"
                    );
                }

                return RedirectToAction(nameof(Index));
            }

            // If invalid, repopulate dropdowns if needed
            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                ViewBag.Workers = new SelectList(_context.Workers, "Id", "FullName", leaveRequest.WorkerId);
            }

            return View(leaveRequest);
        }

        // GET: LeaveRequests/Review/5
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> Review(int? id)
        {
            if (id == null) return NotFound();

            var leaveRequest = await _context.LeaveRequests
                .Include(l => l.Worker)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leaveRequest == null) return NotFound();

            return View(leaveRequest);
        }

        // POST: LeaveRequests/Review/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> Review(int id, string status, string reviewComment)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(l => l.Worker)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leaveRequest == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            leaveRequest.Status = status;
            leaveRequest.ReviewComment = reviewComment;
            leaveRequest.ReviewedById = user.Id;
            leaveRequest.ReviewedOn = DateTime.Now;

            _context.Update(leaveRequest);
            await _context.SaveChangesAsync();

            // Notify the worker
            if (leaveRequest.Worker != null && !string.IsNullOrEmpty(leaveRequest.Worker.UserId))
            {
                var statusIcon = status == "Approved" ? "✅" : "❌";
                await _notificationService.CreateNotification(
                    $"{statusIcon} Leave Request {status}",
                    $"Your {leaveRequest.LeaveType} leave request from {leaveRequest.StartDate.ToShortDateString()} to {leaveRequest.EndDate.ToShortDateString()} has been {status.ToLower()}.",
                    "LeaveRequest",
                    leaveRequest.Worker.UserId,
                    leaveRequest.Id,
                    "LeaveRequest"
                );
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: LeaveRequests/Delete/5
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var leaveRequest = await _context.LeaveRequests
                .Include(l => l.Worker)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leaveRequest == null) return NotFound();

            return View(leaveRequest);
        }

        // POST: LeaveRequests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest != null)
            {
                _context.LeaveRequests.Remove(leaveRequest);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}