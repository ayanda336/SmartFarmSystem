using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Roles = "Admin, Manager")]
    public class ExpensesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public ExpensesController(
            ApplicationDbContext context,
            INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET: Expenses
        public async Task<IActionResult> Index()
        {
            var expenses = _context.Expenses
                .Include(e => e.Project)
                .OrderByDescending(e => e.ExpenseDate);
            return View(await expenses.ToListAsync());
        }

        // GET: Expenses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var expense = await _context.Expenses
                .Include(e => e.Project)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (expense == null)
            {
                return NotFound();
            }

            return View(expense);
        }

        // GET: Expenses/Create
        public IActionResult Create()
        {
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name");
            return View();
        }

        // POST: Expenses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Description,Amount,ExpenseDate,Category,Reference,ProjectId")] Expense expense)
        {
            if (ModelState.IsValid)
            {
                if (expense.ExpenseDate == default)
                {
                    expense.ExpenseDate = DateTime.Now;
                }

                _context.Add(expense);
                await _context.SaveChangesAsync();

                // ===== NOTIFICATION: New Expense Recorded =====
                var project = await _context.Projects.FindAsync(expense.ProjectId);
                await _notificationService.CreateNotification(
                    $"💸 New Expense: {expense.Description}",
                    $"A new expense was recorded: {expense.Description} for R{expense.Amount:N2}. " +
                    $"Category: {expense.Category ?? "Other"}. Project: {project?.Name}.",
                    "ExpenseRecorded",
                    null,
                    expense.Id,
                    "Expense"
                );

                // ===== NOTIFICATION: Large Expense (> R5,000) =====
                if (expense.Amount > 5000)
                {
                    await _notificationService.CreateNotification(
                        $"⚠️ Large Expense: R{expense.Amount:N2}",
                        $"A large expense of R{expense.Amount:N2} was recorded for {expense.Description}. " +
                        $"Project: {project?.Name}. Please review if this is expected.",
                        "LargeExpense",
                        null,
                        expense.Id,
                        "Expense"
                    );
                }

                return RedirectToAction(nameof(Index));
            }
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", expense.ProjectId);
            return View(expense);
        }

        // GET: Expenses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null)
            {
                return NotFound();
            }
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", expense.ProjectId);
            return View(expense);
        }

        // POST: Expenses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Description,Amount,ExpenseDate,Category,Reference,ProjectId")] Expense expense)
        {
            if (id != expense.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(expense);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExpenseExists(expense.Id))
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
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", expense.ProjectId);
            return View(expense);
        }

        // GET: Expenses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var expense = await _context.Expenses
                .Include(e => e.Project)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (expense == null)
            {
                return NotFound();
            }

            return View(expense);
        }

        // POST: Expenses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense != null)
            {
                _context.Expenses.Remove(expense);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ExpenseExists(int id)
        {
            return _context.Expenses.Any(e => e.Id == id);
        }
    }
}