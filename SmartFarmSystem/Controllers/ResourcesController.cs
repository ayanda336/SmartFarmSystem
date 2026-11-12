using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartFarmSystem.Data;
using SmartFarmSystem.Models;
using SmartFarmSystem.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartFarmSystem.Controllers
{
    [Authorize(Roles = "Admin, Manager")]
    public class ResourcesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public ResourcesController(
            ApplicationDbContext context,
            INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET: Resources
        public async Task<IActionResult> Index()
        {
            return View(await _context.Resources.ToListAsync());
        }

        // GET: Resources/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resource = await _context.Resources
                .FirstOrDefaultAsync(m => m.Id == id);
            if (resource == null)
            {
                return NotFound();
            }

            return View(resource);
        }

        // GET: Resources/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Resources/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Quantity,Unit,MinimumStock,UnitPrice,LastRestockedDate")] Resource resource, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                // Handle image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                    if (allowedExtensions.Contains(fileExtension))
                    {
                        var fileName = $"{Guid.NewGuid()}{fileExtension}";
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "resources", fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }

                        resource.ImagePath = $"/images/resources/{fileName}";
                    }
                    else
                    {
                        ModelState.AddModelError("imageFile", "Please upload a valid image file (jpg, png, gif, webp)");
                        return View(resource);
                    }
                }

                if (resource.LastRestockedDate == null)
                {
                    resource.LastRestockedDate = DateTime.Now;
                }

                _context.Add(resource);
                await _context.SaveChangesAsync();

                // ===== NOTIFICATION: Low Stock Alert =====
                if (resource.Quantity <= resource.MinimumStock)
                {
                    await _notificationService.CreateNotification(
                        $"⚠️ Low Stock Alert: {resource.Name}",
                        $"{resource.Name} is running low! Current stock: {resource.Quantity} {resource.Unit}. Minimum required: {resource.MinimumStock} {resource.Unit}. Please restock soon.",
                        "LowStock",
                        null,
                        resource.Id,
                        "Resource"
                    );
                }

                return RedirectToAction(nameof(Index));
            }
            return View(resource);
        }

        // GET: Resources/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resource = await _context.Resources.FindAsync(id);
            if (resource == null)
            {
                return NotFound();
            }
            return View(resource);
        }

        // POST: Resources/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Quantity,Unit,MinimumStock,UnitPrice,LastRestockedDate,ImagePath")] Resource resource, IFormFile? imageFile)
        {
            if (id != resource.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get original resource before update
                    var originalResource = await _context.Resources
                        .AsNoTracking()
                        .FirstOrDefaultAsync(r => r.Id == id);

                    // Handle image upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                        var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                        if (allowedExtensions.Contains(fileExtension))
                        {
                            if (!string.IsNullOrEmpty(resource.ImagePath))
                            {
                                var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", resource.ImagePath.TrimStart('/'));
                                if (System.IO.File.Exists(oldImagePath))
                                {
                                    System.IO.File.Delete(oldImagePath);
                                }
                            }

                            var fileName = $"{Guid.NewGuid()}{fileExtension}";
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "resources", fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await imageFile.CopyToAsync(stream);
                            }

                            resource.ImagePath = $"/images/resources/{fileName}";
                        }
                        else
                        {
                            ModelState.AddModelError("imageFile", "Please upload a valid image file (jpg, png, gif, webp)");
                            return View(resource);
                        }
                    }

                    _context.Update(resource);
                    await _context.SaveChangesAsync();

                    // ===== NOTIFICATION: Low Stock Alert =====
                    if (resource.Quantity <= resource.MinimumStock)
                    {
                        await _notificationService.CreateNotification(
                            $"⚠️ Low Stock Alert: {resource.Name}",
                            $"{resource.Name} is running low! Current stock: {resource.Quantity} {resource.Unit}. Minimum required: {resource.MinimumStock} {resource.Unit}. Please restock soon.",
                            "LowStock",
                            null,
                            resource.Id,
                            "Resource"
                        );
                    }
                    // ===== NOTIFICATION: Restocked =====
                    else if (originalResource != null &&
                             originalResource.Quantity <= originalResource.MinimumStock &&
                             resource.Quantity > resource.MinimumStock)
                    {
                        await _notificationService.CreateNotification(
                            $"✅ Restocked: {resource.Name}",
                            $"{resource.Name} has been restocked! Current stock: {resource.Quantity} {resource.Unit}.",
                            "Restocked",
                            null,
                            resource.Id,
                            "Resource"
                        );
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ResourceExists(resource.Id))
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
            return View(resource);
        }

        // GET: Resources/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resource = await _context.Resources
                .FirstOrDefaultAsync(m => m.Id == id);
            if (resource == null)
            {
                return NotFound();
            }

            return View(resource);
        }

        // POST: Resources/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource != null)
            {
                // ===== NOTIFICATION: Resource Deleted =====
                await _notificationService.CreateNotification(
                    $"🗑️ Resource Removed: {resource.Name}",
                    $"Resource '{resource.Name}' has been removed from inventory. Quantity: {resource.Quantity} {resource.Unit}.",
                    "ResourceDeleted",
                    null,
                    resource.Id,
                    "Resource"
                );

                if (!string.IsNullOrEmpty(resource.ImagePath))
                {
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", resource.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Resources.Remove(resource);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ResourceExists(int id)
        {
            return _context.Resources.Any(e => e.Id == id);
        }
    }
}