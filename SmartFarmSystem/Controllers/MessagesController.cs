using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly INotificationService _notificationService;

        public MessagesController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: Messages/Inbox
        public async Task<IActionResult> Inbox()
        {
            var user = await _userManager.GetUserAsync(User);
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.ReceiverId == user.Id && !m.IsDeletedByReceiver)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            return View(messages);
        }

        // GET: Messages/Sent
        public async Task<IActionResult> Sent()
        {
            var user = await _userManager.GetUserAsync(User);
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.SenderId == user.Id && !m.IsDeletedBySender)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            return View(messages);
        }

        // GET: Messages/Compose
        public async Task<IActionResult> Compose(string? receiverId = null)
        {
            ViewBag.ReceiverId = receiverId;
            ViewBag.Users = await _userManager.Users.ToListAsync();
            return View();
        }

        // POST: Messages/Compose
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Compose(string receiverId, string subject, string body, int? parentMessageId = null)
        {
            if (string.IsNullOrEmpty(receiverId))
            {
                ModelState.AddModelError("", "Please select a recipient.");
                ViewBag.Users = await _userManager.Users.ToListAsync();
                return View();
            }

            var user = await _userManager.GetUserAsync(User);

            var message = new Message
            {
                SenderId = user.Id,
                ReceiverId = receiverId,
                Subject = subject,
                Body = body,
                SentAt = DateTime.Now,
                IsRead = false,
                ParentMessageId = parentMessageId
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Send notification to receiver
            await _notificationService.CreateNotification(
                $"📩 New Message: {subject}",
                $"You have received a new message from {user.Email}: {subject}",
                "Message",
                receiverId,
                message.Id,
                "Message"
            );

            return RedirectToAction(nameof(Sent));
        }

        // GET: Messages/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var message = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Include(m => m.ParentMessage)
                .ThenInclude(p => p.Sender)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null) return NotFound();

            // Mark as read if user is the receiver
            if (message.ReceiverId == user.Id && !message.IsRead)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return View(message);
        }

        // GET: Messages/Reply/5
        public async Task<IActionResult> Reply(int? id)
        {
            if (id == null) return NotFound();

            var original = await _context.Messages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (original == null) return NotFound();

            ViewBag.Original = original;
            ViewBag.ReceiverId = original.SenderId;
            ViewBag.Subject = original.Subject.StartsWith("Re:") ? original.Subject : $"Re: {original.Subject}";

            return View();
        }

        // POST: Messages/Reply/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string receiverId, string subject, string body)
        {
            var user = await _userManager.GetUserAsync(User);

            var message = new Message
            {
                SenderId = user.Id,
                ReceiverId = receiverId,
                Subject = subject,
                Body = body,
                SentAt = DateTime.Now,
                IsRead = false,
                ParentMessageId = id
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Send notification to receiver
            await _notificationService.CreateNotification(
                $"📩 Reply: {subject}",
                $"You have received a reply from {user.Email}: {subject}",
                "Message",
                receiverId,
                message.Id,
                "Message"
            );

            return RedirectToAction(nameof(Sent));
        }

        // POST: Messages/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var message = await _context.Messages.FindAsync(id);

            if (message == null) return NotFound();

            if (message.SenderId == user.Id)
            {
                message.IsDeletedBySender = true;
            }
            else if (message.ReceiverId == user.Id)
            {
                message.IsDeletedByReceiver = true;
            }

            // Permanently delete if both parties deleted
            if (message.IsDeletedBySender && message.IsDeletedByReceiver)
            {
                _context.Messages.Remove(message);
            }
            else
            {
                _context.Update(message);
            }

            await _context.SaveChangesAsync();
            return Redirect(returnUrl ?? Url.Action(nameof(Inbox)));
        }

        // GET: Messages/UnreadCount (for AJAX)
        public async Task<IActionResult> UnreadCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(0);

            var count = await _context.Messages
                .CountAsync(m => m.ReceiverId == user.Id && !m.IsRead && !m.IsDeletedByReceiver);

            return Json(count);
        }
    }
}