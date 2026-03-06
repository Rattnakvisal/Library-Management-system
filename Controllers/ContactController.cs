using Library_Management_system.Data;
using Library_Management_system.Models;
using Library_Management_system.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Library_Management_system.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IContactTelegramNotifier _contactTelegramNotifier;

        public ContactController(
            ApplicationDbContext context,
            IContactTelegramNotifier contactTelegramNotifier)
        {
            _context = context;
            _contactTelegramNotifier = contactTelegramNotifier;
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

        [HttpGet("contact/telegram")]
        public async Task<IActionResult> OpenTelegramContact()
        {
            var actor = ResolveActor();
            await _contactTelegramNotifier.SendAdminAlertAsync(
                string.Join('\n',
                    "User opened contact bot.",
                    $"Actor: {actor}",
                    "Expected Flow: welcome + questions",
                    $"Opened (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"));

            var redirectUrl = _contactTelegramNotifier.BuildWelcomeUrl();
            return Redirect(redirectUrl);
        }

        private string ResolveActor()
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return "Anonymous user";
            }

            var email = User.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrWhiteSpace(email))
            {
                return email.Trim();
            }

            var name = User.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name.Trim();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrWhiteSpace(userId) ? "Authenticated user" : $"UserId: {userId}";
        }

        public sealed class ContactFeedbackInput
        {
            public string Email { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }
    }
}
