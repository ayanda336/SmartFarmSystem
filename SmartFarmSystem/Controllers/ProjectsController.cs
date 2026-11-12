using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartFarmSystem.Data;
using SmartFarmSystem.Models;
using SmartFarmSystem.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartFarmSystem.Controllers
{
    [Authorize(Roles = "Admin, Manager")]
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public ProjectsController(
            ApplicationDbContext context,
            INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            return View(await _context.Projects.ToListAsync());
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .FirstOrDefaultAsync(m => m.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // GET: Projects/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,StartDate,EndDate,Status,CreatedAt")] Project project)
        {
            if (ModelState.IsValid)
            {
                if (project.CreatedAt == default)
                {
                    project.CreatedAt = DateTime.Now;
                }
                _context.Add(project);
                await _context.SaveChangesAsync();

                // ===== NOTIFICATION: New Project Created =====
                await _notificationService.CreateNotification(
                    $"🌱 New Project: {project.Name}",
                    $"A new project '{project.Name}' has been created. Start date: {project.StartDate.ToShortDateString()}. End date: {project.EndDate.ToShortDateString()}.",
                    "ProjectCreated",
                    null,
                    project.Id,
                    "Project"
                );

                return RedirectToAction(nameof(Index));
            }
            return View(project);
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }
            return View(project);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,StartDate,EndDate,Status,CreatedAt")] Project project)
        {
            if (id != project.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var originalProject = await _context.Projects
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == id);

                    _context.Update(project);
                    await _context.SaveChangesAsync();

                    // ===== NOTIFICATION: Project Status Changed =====
                    if (originalProject != null && originalProject.Status != project.Status)
                    {
                        await _notificationService.CreateNotification(
                            $"🔄 Project Status Updated: {project.Name}",
                            $"Project '{project.Name}' status changed from '{originalProject.Status}' to '{project.Status}'.",
                            "ProjectUpdated",
                            null,
                            project.Id,
                            "Project"
                        );
                    }

                    // ===== NOTIFICATION: Project Nearing End Date =====
                    if (project.EndDate <= DateTime.Now.AddDays(7) && project.Status != "Completed")
                    {
                        await _notificationService.CreateNotification(
                            $"⏰ Project Ending Soon: {project.Name}",
                            $"Project '{project.Name}' is ending soon on {project.EndDate.ToShortDateString()}. Please ensure all tasks are completed.",
                            "ProjectReminder",
                            null,
                            project.Id,
                            "Project"
                        );
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.Id))
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
            return View(project);
        }

        // GET: Projects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .FirstOrDefaultAsync(m => m.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project != null)
            {
                _context.Projects.Remove(project);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }
    }
}