using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartFarmSystem.Data;
using SmartFarmSystem.Models;
using System.Threading.Tasks;

namespace SmartFarmSystem.Controllers
{
    // ONLY Admins and Managers can manage workers
    [Authorize(Roles = "Admin, Manager")]
    public class WorkersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WorkersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Workers
        public async Task<IActionResult> Index()
        {
            // Include the User so we can show associated login email
            var workers = await _context.Workers.Include(w => w.User).ToListAsync();
            return View(workers);
        }

        // GET: Workers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var worker = await _context.Workers
                .Include(w => w.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (worker == null)
            {
                return NotFound();
            }

            return View(worker);
        }

        // GET: Workers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Workers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FullName,PhoneNumber,HireDate,IsActive,UserId")] Worker worker)
        {
            if (ModelState.IsValid)
            {
                // 1. Add the worker to the database FIRST to get the auto-generated Id
                _context.Add(worker);
                await _context.SaveChangesAsync();

                // 2. Generate the Employee Code using the new Id
                // Format: WORK-1001 (Id 1 + 1000 = 1001)
                int employeeNumber = worker.Id + 1000;
                worker.EmployeeCode = $"WORK-{employeeNumber}";

                // 3. Update the record with the generated code
                _context.Update(worker);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(worker);
        }

        // GET: Workers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var worker = await _context.Workers.FindAsync(id);
            if (worker == null)
            {
                return NotFound();
            }
            return View(worker);
        }

        // POST: Workers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,PhoneNumber,HireDate,IsActive")] Worker worker) // Removed EmployeeCode and UserId from Bind to prevent overwriting
        {
            if (id != worker.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Fetch the existing worker from the database
                    var existingWorker = await _context.Workers.FindAsync(id);
                    if (existingWorker == null)
                    {
                        return NotFound();
                    }

                    // 2. Update ONLY the fields that should be editable
                    existingWorker.FullName = worker.FullName;
                    existingWorker.PhoneNumber = worker.PhoneNumber;
                    existingWorker.HireDate = worker.HireDate;
                    existingWorker.IsActive = worker.IsActive;
                    // DO NOT update EmployeeCode or UserId here! Keep the original.

                    // 3. Save changes
                    _context.Update(existingWorker);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WorkerExists(worker.Id))
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
            return View(worker);
        }

        // GET: Workers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var worker = await _context.Workers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (worker == null)
            {
                return NotFound();
            }

            return View(worker);
        }

        // POST: Workers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var worker = await _context.Workers.FindAsync(id);
            if (worker != null)
            {
                _context.Workers.Remove(worker);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool WorkerExists(int id)
        {
            return _context.Workers.Any(e => e.Id == id);
        }
    }
}