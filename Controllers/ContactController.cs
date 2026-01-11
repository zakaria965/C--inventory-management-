using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Contact
        public IActionResult Index()
        {
            // Allow public access - no login required
            return View();
        }

        // POST: Contact/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Validate phone number format (basic validation)
                if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
                {
                    // Remove spaces, dashes, and parentheses for validation
                    string cleanPhone = Regex.Replace(model.PhoneNumber, @"[\s\-\(\)]", "");
                    if (cleanPhone.Length < 10 || cleanPhone.Length > 15)
                    {
                        ModelState.AddModelError("PhoneNumber", "Phone number must be between 10 and 15 digits.");
                        return View(model);
                    }
                }

                // Check for duplicate submissions (same email and content within last 5 minutes)
                var recentDuplicate = await _context.Messages
                    .Where(m => m.EmailAddress == model.EmailAddress 
                        && m.Content == model.MessageContent
                        && m.CreatedDate >= DateTime.Now.AddMinutes(-5))
                    .FirstOrDefaultAsync();

                if (recentDuplicate != null)
                {
                    ModelState.AddModelError("", "You have already submitted this message recently. Please wait a few minutes before submitting again.");
                    return View(model);
                }

                // Create message from contact form
                var message = new Message
                {
                    Title = $"Contact: {model.Subject}",
                    Content = $"From: {model.CustomerName}\nEmail: {model.EmailAddress}\nPhone: {model.PhoneNumber}\n\nMessage:\n{model.MessageContent}",
                    CustomerName = model.CustomerName,
                    PhoneNumber = model.PhoneNumber,
                    EmailAddress = model.EmailAddress,
                    Subject = model.Subject,
                    MessageType = "Contact",
                    IsRead = false,
                    IsImportant = false,
                    CreatedDate = DateTime.Now
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Thank you for contacting us! Your message has been sent successfully. We will get back to you soon.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
    }

    public class ContactViewModel
    {
        [Required(ErrorMessage = "Customer name is required")]
        [Display(Name = "Customer Name")]
        [StringLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Display(Name = "Phone Number")]
        [StringLength(20)]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [Display(Name = "Email Address")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(200)]
        public string EmailAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required")]
        [Display(Name = "Subject")]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message content is required")]
        [Display(Name = "Message Content")]
        [StringLength(2000)]
        public string MessageContent { get; set; } = string.Empty;
    }
}

