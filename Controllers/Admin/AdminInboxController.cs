using Library_Management_system.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_system.Controllers.Admin
{
    [Authorize(Roles = "Admin,Librarian")]
    [Route("admin/inbox")]
    public class AdminInboxController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminInboxController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("contacts/{id:int}/mark-read")]
        public async Task<IActionResult> MarkContactAsRead(int id)
        {
            var message = await _context.ContactMessages.FirstOrDefaultAsync(x => x.Id == id);
            if (message == null)
            {
                return NotFound(new { success = false, message = "Contact message not found." });
            }

            if (!message.IsRead)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            var unreadCount = await _context.ContactMessages.CountAsync(x => !x.IsRead);
            return Ok(new { success = true, unreadCount });
        }

        [HttpPost("contacts/mark-all-read")]
        public async Task<IActionResult> MarkAllContactsAsRead()
        {
            var unreadMessages = await _context.ContactMessages
                .Where(x => !x.IsRead)
                .ToListAsync();

            if (unreadMessages.Count > 0)
            {
                foreach (var item in unreadMessages)
                {
                    item.IsRead = true;
                }

                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true, unreadCount = 0 });
        }
    }
}
