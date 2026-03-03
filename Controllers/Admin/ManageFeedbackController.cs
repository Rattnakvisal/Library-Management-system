using Library_Management_system.Data;
using Library_Management_system.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_system.Controllers.Admin;

[Authorize(Roles = "Admin,Librarian")]
public class ManageFeedbackController : Controller
{
    private const int DefaultPageSize = 10;
    private readonly ApplicationDbContext _context;

    public ManageFeedbackController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("admin/managefeedback")]
    public async Task<IActionResult> Index(string? q = null, int page = 1)
    {
        var search = (q ?? string.Empty).Trim();
        var pageIndex = Math.Max(1, page);

        var query = _context.ContactMessages.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Email.Contains(search) || x.Message.Contains(search));
        }

        var totalMessages = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalMessages / (double)DefaultPageSize));
        pageIndex = Math.Min(pageIndex, totalPages);

        var messages = await query
            .OrderBy(x => x.IsRead)
            .ThenByDescending(x => x.CreatedDate)
            .Skip((pageIndex - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .Select(x => new ManageFeedbackMessageItemViewModel
            {
                Id = x.Id,
                Email = x.Email,
                Message = x.Message,
                IsRead = x.IsRead,
                CreatedDate = x.CreatedDate
            })
            .ToListAsync();

        var unreadMessages = await _context.ContactMessages
            .AsNoTracking()
            .CountAsync(x => !x.IsRead);

        var model = new ManageFeedbackPageViewModel
        {
            Search = search,
            CurrentPage = pageIndex,
            TotalPages = totalPages,
            PageSize = DefaultPageSize,
            TotalMessages = totalMessages,
            UnreadMessages = unreadMessages,
            Messages = messages
        };

        return View("~/Views/Admin/ManageFeedback/Index.cshtml", model);
    }

    [HttpPost("admin/managefeedback/read/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id, string? q = null, int page = 1)
    {
        var message = await _context.ContactMessages.FirstOrDefaultAsync(x => x.Id == id);
        if (message != null && !message.IsRead)
        {
            message.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index), new { q, page });
    }

    [HttpPost("admin/managefeedback/mark-all-read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead(string? q = null, int page = 1)
    {
        var unreadMessages = await _context.ContactMessages
            .Where(x => !x.IsRead)
            .ToListAsync();

        if (unreadMessages.Count > 0)
        {
            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index), new { q, page });
    }

    [HttpPost("admin/managefeedback/delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? q = null, int page = 1)
    {
        var message = await _context.ContactMessages.FirstOrDefaultAsync(x => x.Id == id);
        if (message != null)
        {
            _context.ContactMessages.Remove(message);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index), new { q, page });
    }
}
