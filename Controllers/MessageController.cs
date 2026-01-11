using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Controllers
{
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MessageController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Message
        public async Task<IActionResult> Index(bool? isRead, bool? isImportant)
        {
            var role = HttpContext.Session.GetString("UserRole") ?? "User";
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var messages = from m in _context.Messages select m;

            if (isRead.HasValue)
            {
                messages = messages.Where(m => m.IsRead == isRead.Value);
            }

            if (isImportant.HasValue && isImportant.Value)
            {
                messages = messages.Where(m => m.IsImportant);
            }

            // If not admin, limit to messages sent by (or to) this user
            if (role.ToLower() != "admin")
            {
                if (!string.IsNullOrWhiteSpace(userEmail))
                {
                    messages = messages.Where(m => m.EmailAddress == userEmail || m.CustomerName == HttpContext.Session.GetString("UserName"));
                }
                else
                {
                    messages = messages.Where(m => false); // no messages
                }
            }

            var unreadCount = await _context.Messages.Where(m => !m.IsRead).CountAsync();
            ViewBag.UnreadCount = unreadCount;

            return View(await messages.OrderByDescending(m => m.CreatedDate).ToListAsync());
        }

        // GET: Message/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Message/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Message message)
        {
            if (ModelState.IsValid)
            {
                message.CreatedDate = DateTime.Now;

                // Populate sender details for logged-in users
                var userEmail = HttpContext.Session.GetString("UserEmail");
                var userName = HttpContext.Session.GetString("UserName");
                if (string.IsNullOrWhiteSpace(message.EmailAddress) && !string.IsNullOrWhiteSpace(userEmail)) message.EmailAddress = userEmail;
                if (string.IsNullOrWhiteSpace(message.CustomerName) && !string.IsNullOrWhiteSpace(userName)) message.CustomerName = userName;

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Message created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(message);
        }

        // GET: Message/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var message = await _context.Messages.FindAsync(id);
            if (message == null)
            {
                return NotFound();
            }

            // Mark as read when viewing
            if (!message.IsRead)
            {
                message.IsRead = true;
                message.ReadDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return View(message);
        }

        // POST: Message/MarkAsRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message != null)
            {
                message.IsRead = true;
                message.ReadDate = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Message marked as read!";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Message/MarkAsUnread/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsUnread(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message != null)
            {
                message.IsRead = false;
                message.ReadDate = null;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Message marked as unread!";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Message/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null)
            {
                return NotFound();
            }

            return View(message);
        }

        // POST: Message/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message != null)
            {
                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Message deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

