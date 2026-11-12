using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartFarmSystem.Data;
using SmartFarmSystem.Models;
using SmartFarmSystem.Services;
using System.Linq;
using System.Threading.Tasks;

namespace SmartFarmSystem.Controllers
{
    [Authorize(Roles = "Admin, Manager")]
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly UserManager<IdentityUser> _userManager;

        public TasksController(
            ApplicationDbContext context,
            INotificationService notificationService,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _notificationService = notificationService;
            _userManager = userManager;
        }

        // GET: Tasks
        public async Task<IActionResult> Index()
        {
            var taskItems = _context.TaskItems
                .Include(t => t.Project)
                .Include(t => t.Worker)
                .Include(t => t.Resource);
            return View(await taskItems.ToListAsync());
        }

        // GET: Tasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taskItem = await _context.TaskItems
                .Include(t => t.Project)
                .Include(t => t.Worker)
                .Include(t => t.Resource)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (taskItem == null)
            {
                return NotFound();
            }

            return View(taskItem);
        }

        // GET: Tasks/Create
        public IActionResult Create()
        {
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name");
            ViewData["WorkerId"] = new SelectList(_context.Workers, "Id", "FullName");
            ViewData["ResourceId"] = new SelectList(_context.Resources, "Id", "Name");
            return View();
        }

        // POST: Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,AssignedDate,DueDate,Status,ProjectId,WorkerId,ResourceId,QuantityNeeded")] TaskItem taskItem)
        {
            if (ModelState.IsValid)
            {
                if (taskItem.AssignedDate == default)
                {
                    taskItem.AssignedDate = DateTime.Now;
                }

                if (string.IsNullOrEmpty(taskItem.Status))
                {
                    taskItem.Status = "Pending";
                }

                _context.Add(taskItem);
                await _context.SaveChangesAsync();

                // ===== AUTO-NOTIFICATION: Notify Worker of New Task =====
                var worker = await _context.Workers
                    .Include(w => w.User)
                    .FirstOrDefaultAsync(w => w.Id == taskItem.WorkerId);

                if (worker != null && !string.IsNullOrEmpty(worker.UserId))
                {
                    var project = await _context.Projects.FindAsync(taskItem.ProjectId);
                    await _notificationService.CreateNotification(
                        $"📋 New Task: {taskItem.Title}",
                        $"You have been assigned a new task: '{taskItem.Title}' for project '{project?.Name}'. " +
                        $"Due date: {taskItem.DueDate?.ToShortDateString() ?? "Not set"}.",
                        "TaskAssigned",
                        worker.UserId,
                        taskItem.Id,
                        "Task"
                    );
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", taskItem.ProjectId);
            ViewData["WorkerId"] = new SelectList(_context.Workers, "Id", "FullName", taskItem.WorkerId);
            ViewData["ResourceId"] = new SelectList(_context.Resources, "Id", "Name", taskItem.ResourceId);
            return View(taskItem);
        }

        // GET: Tasks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taskItem = await _context.TaskItems.FindAsync(id);
            if (taskItem == null)
            {
                return NotFound();
            }

            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", taskItem.ProjectId);
            ViewData["WorkerId"] = new SelectList(_context.Workers, "Id", "FullName", taskItem.WorkerId);
            ViewData["ResourceId"] = new SelectList(_context.Resources, "Id", "Name", taskItem.ResourceId);
            return View(taskItem);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,AssignedDate,DueDate,Status,ProjectId,WorkerId,ResourceId,QuantityNeeded")] TaskItem taskItem)
        {
            if (id != taskItem.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var originalTask = await _context.TaskItems
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == id);

                    _context.Update(taskItem);
                    await _context.SaveChangesAsync();

                    // ===== AUTO-NOTIFICATION: Notify if worker changed =====
                    if (originalTask != null && originalTask.WorkerId != taskItem.WorkerId)
                    {
                        var worker = await _context.Workers
                            .Include(w => w.User)
                            .FirstOrDefaultAsync(w => w.Id == taskItem.WorkerId);

                        if (worker != null && !string.IsNullOrEmpty(worker.UserId))
                        {
                            var project = await _context.Projects.FindAsync(taskItem.ProjectId);
                            await _notificationService.CreateNotification(
                                $"🔄 Task Reassigned: {taskItem.Title}",
                                $"Task '{taskItem.Title}' for project '{project?.Name}' has been reassigned to you. " +
                                $"Due date: {taskItem.DueDate?.ToShortDateString() ?? "Not set"}.",
                                "TaskAssigned",
                                worker.UserId,
                                taskItem.Id,
                                "Task"
                            );
                        }
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskItemExists(taskItem.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", taskItem.ProjectId);
            ViewData["WorkerId"] = new SelectList(_context.Workers, "Id", "FullName", taskItem.WorkerId);
            ViewData["ResourceId"] = new SelectList(_context.Resources, "Id", "Name", taskItem.ResourceId);
            return View(taskItem);
        }

        // GET: Tasks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taskItem = await _context.TaskItems
                .Include(t => t.Project)
                .Include(t => t.Worker)
                .Include(t => t.Resource)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (taskItem == null)
            {
                return NotFound();
            }

            return View(taskItem);
        }

        // POST: Tasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var taskItem = await _context.TaskItems.FindAsync(id);
            if (taskItem != null)
            {
                _context.TaskItems.Remove(taskItem);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TaskItemExists(int id)
        {
            return _context.TaskItems.Any(e => e.Id == id);
        }
    }
}