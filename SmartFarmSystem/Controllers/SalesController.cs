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
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public SalesController(
            ApplicationDbContext context,
            INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var sales = _context.Sales.Include(s => s.Project).OrderByDescending(s => s.SaleDate);
            return View(await sales.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var sale = await _context.Sales.Include(s => s.Project).FirstOrDefaultAsync(m => m.Id == id);
            if (sale == null) return NotFound();
            return View(sale);
        }

        public IActionResult Create()
        {
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ProductName,Quantity,Unit,UnitPrice,SaleDate,BuyerName,BuyerContact,Reference,ProjectId")] Sale sale)
        {
            if (ModelState.IsValid)
            {
                sale.TotalAmount = sale.Quantity * sale.UnitPrice;
                if (sale.SaleDate == default) sale.SaleDate = DateTime.Now;

                _context.Add(sale);
                await _context.SaveChangesAsync();

                // ===== NOTIFICATION: New Sale Recorded =====
                var project = await _context.Projects.FindAsync(sale.ProjectId);
                await _notificationService.CreateNotification(
                    $"💰 New Sale: {sale.ProductName}",
                    $"A new sale was recorded: {sale.Quantity} {sale.Unit} of {sale.ProductName} for R{sale.TotalAmount:N2}. " +
                    $"Project: {project?.Name}. Buyer: {sale.BuyerName ?? "Not specified"}.",
                    "SaleRecorded",
                    null,
                    sale.Id,
                    "Sale"
                );

                // ===== NOTIFICATION: High Value Sale (> R10,000) =====
                if (sale.TotalAmount > 10000)
                {
                    await _notificationService.CreateNotification(
                        $"💰 High Value Sale: R{sale.TotalAmount:N2}",
                        $"A high-value sale of {sale.ProductName} was recorded for R{sale.TotalAmount:N2}. " +
                        $"Buyer: {sale.BuyerName ?? "Not specified"}. Project: {project?.Name}.",
                        "HighValueSale",
                        null,
                        sale.Id,
                        "Sale"
                    );
                }

                return RedirectToAction(nameof(Index));
            }
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", sale.ProjectId);
            return View(sale);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var sale = await _context.Sales.FindAsync(id);
            if (sale == null) return NotFound();
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", sale.ProjectId);
            return View(sale);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ProductName,Quantity,Unit,UnitPrice,SaleDate,BuyerName,BuyerContact,Reference,ProjectId")] Sale sale)
        {
            if (id != sale.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    sale.TotalAmount = sale.Quantity * sale.UnitPrice;
                    _context.Update(sale);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SaleExists(sale.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", sale.ProjectId);
            return View(sale);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var sale = await _context.Sales.Include(s => s.Project).FirstOrDefaultAsync(m => m.Id == id);
            if (sale == null) return NotFound();
            return View(sale);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sale = await _context.Sales.FindAsync(id);
            if (sale != null) _context.Sales.Remove(sale);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SaleExists(int id) => _context.Sales.Any(e => e.Id == id);
    }
}