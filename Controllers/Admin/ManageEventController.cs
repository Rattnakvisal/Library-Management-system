using Library_Management_system.Data;
using Library_Management_system.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_system.Controllers.Admin;

[Authorize(Roles = "Admin,Librarian")]
public class ManageEventController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ManageEventController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet("admin/manageevent")]
    public async Task<IActionResult> Index()
    {
        var events = await _context.Events
            .AsNoTracking()
            .OrderByDescending(e => e.StartDate)
            .ThenByDescending(e => e.Id)
            .ToListAsync();

        return View("~/Views/Admin/ManageEvent/Index.cshtml", events);
    }

    [HttpPost("admin/manageevent/add")]
    public async Task<IActionResult> Add([FromForm] UpsertEventRequest request)
    {
        var validationError = ValidateRequest(request);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            return BadRequest(new { success = false, message = validationError });
        }

        var imageUrl = await SaveEventImageAsync(request.EventImage);
        var nowUtc = DateTime.UtcNow;
        var startDate = request.StartDate.GetValueOrDefault().Date;
        var endDate = request.EndDate.GetValueOrDefault().Date;

        var entity = new LibraryEvent
        {
            Name = request.EventName.Trim(),
            Description = request.Description.Trim(),
            Location = request.Location.Trim(),
            StartDate = startDate,
            EndDate = endDate,
            ImageUrl = imageUrl,
            CreatedBy = User?.Identity?.Name,
            CreatedDate = nowUtc
        };

        _context.Events.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Event added successfully." });
    }

    [HttpPost("admin/manageevent/update/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromForm] UpsertEventRequest request)
    {
        var validationError = ValidateRequest(request);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            return BadRequest(new { success = false, message = validationError });
        }

        var entity = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (entity == null)
        {
            return NotFound(new { success = false, message = "Event not found." });
        }

        var startDate = request.StartDate.GetValueOrDefault().Date;
        var endDate = request.EndDate.GetValueOrDefault().Date;

        entity.Name = request.EventName.Trim();
        entity.Description = request.Description.Trim();
        entity.Location = request.Location.Trim();
        entity.StartDate = startDate;
        entity.EndDate = endDate;

        if (request.EventImage is { Length: > 0 })
        {
            var imageUrl = await SaveEventImageAsync(request.EventImage);
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                entity.ImageUrl = imageUrl;
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Event updated successfully." });
    }

    [HttpPost("admin/manageevent/delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (entity == null)
        {
            return NotFound(new { success = false, message = "Event not found." });
        }

        _context.Events.Remove(entity);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Event deleted successfully." });
    }

    private static string? ValidateRequest(UpsertEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EventName) ||
            string.IsNullOrWhiteSpace(request.Description) ||
            string.IsNullOrWhiteSpace(request.Location) ||
            !request.StartDate.HasValue ||
            !request.EndDate.HasValue)
        {
            return "Please fill all required fields.";
        }

        if (request.StartDate.Value.Date > request.EndDate.Value.Date)
        {
            return "End date must be after or equal to start date.";
        }

        return null;
    }

    private async Task<string?> SaveEventImageAsync(IFormFile? imageFile)
    {
        if (imageFile is not { Length: > 0 })
        {
            return null;
        }

        var uploadsDirectory = Path.Combine(_environment.WebRootPath, "images", "User", "Event", "uploads");
        Directory.CreateDirectory(uploadsDirectory);

        var extension = Path.GetExtension(imageFile.FileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsDirectory, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await imageFile.CopyToAsync(stream);

        return $"/images/User/Event/uploads/{fileName}";
    }

    public sealed class UpsertEventRequest
    {
        public string EventName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public IFormFile? EventImage { get; set; }
    }
}
