using Library_Management_system.Data;
using Library_Management_system.Models;
using Microsoft.AspNetCore.Mvc;

namespace Library_Management_system.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("contact/send-feedback")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendFeedback(ContactFeedbackInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Message))
            {
                TempData["ContactError"] = "Email and message are required.";
                return RedirectToAction("Contact", "Home");
            }

            var message = new ContactMessage
            {
                Email = input.Email.Trim(),
                Message = input.Message.Trim(),
                IsRead = false,
                CreatedDate = DateTime.UtcNow
            };

            _context.ContactMessages.Add(message);
            await _context.SaveChangesAsync();

            TempData["ContactSuccess"] = "Thank you. Your message has been sent successfully.";
            return RedirectToAction("Contact", "Home");
        }

        public sealed class ContactFeedbackInput
        {
            public string Email { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }
    }
}
