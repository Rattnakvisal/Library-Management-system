using Library_Management_system.Data;
using Library_Management_system.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_system.Controllers.Admin;

public class ManageBooksController : Controller
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "available",
        "unavailable",
        "borrowed",
        "reserved",
        "maintenance"
    };

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ManageBooksController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet("admin/managebooks")]
    public async Task<IActionResult> Index(string? q = null, string? status = null)
    {
        var booksQuery = _context.Books.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            booksQuery = booksQuery.Where(b =>
                b.Title.Contains(keyword) ||
                b.Author.Contains(keyword) ||
                b.CategoryName.Contains(keyword) ||
                b.BookCode.Contains(keyword) ||
                (b.Isbn != null && b.Isbn.Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = NormalizeStatus(status);
            if (!string.IsNullOrWhiteSpace(normalizedStatus))
            {
                booksQuery = booksQuery.Where(b => b.Status == normalizedStatus);
            }
        }

        var books = await booksQuery
            .OrderByDescending(b => b.Id)
            .ToListAsync();

        var categories = await _context.Categories
            .AsNoTracking()
            .Select(c => c.Name)
            .OrderBy(c => c)
            .ToListAsync();

        ViewBag.TotalBooks = books.Count;
        ViewBag.AvailableCopies = books
            .Where(b => string.Equals(b.Status, "available", StringComparison.OrdinalIgnoreCase))
            .Sum(b => b.Quantity);
        ViewBag.UnavailableBooks = books.Count(b =>
            string.Equals(b.Status, "unavailable", StringComparison.OrdinalIgnoreCase) || b.Quantity <= 0);
        ViewBag.BorrowedBooks = books
            .Where(b => string.Equals(b.Status, "borrowed", StringComparison.OrdinalIgnoreCase))
            .Sum(b => b.Quantity);
        ViewBag.BookCategories = categories;

        return View("~/Views/Admin/ManageBooks/Index.cshtml", books);
    }

    [HttpPost("admin/managebooks/add")]
    public async Task<IActionResult> Add([FromForm] AddBookRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BookCode) ||
            string.IsNullOrWhiteSpace(request.BookTitle) ||
            string.IsNullOrWhiteSpace(request.Author) ||
            string.IsNullOrWhiteSpace(request.CategoryName) ||
            !request.Quantity.HasValue ||
            request.Quantity.Value < 0 ||
            !request.Pages.HasValue ||
            request.Pages.Value <= 0 ||
            !request.Year.HasValue ||
            !IsValidYear(request.Year.Value))
        {
            return BadRequest(new { success = false, message = "Please fill all required fields correctly." });
        }

        var normalizedStatus = NormalizeStatus(request.Status);
        if (string.IsNullOrWhiteSpace(normalizedStatus))
        {
            return BadRequest(new { success = false, message = "Please select a valid status." });
        }

        var imageUrl = "/images/User/Book/book2.png";

        if (request.BookImage is { Length: > 0 })
        {
            var uploadsDirectory = Path.Combine(_environment.WebRootPath, "images", "User", "Book", "uploads");
            Directory.CreateDirectory(uploadsDirectory);

            var extension = Path.GetExtension(request.BookImage.FileName);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsDirectory, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await request.BookImage.CopyToAsync(stream);

            imageUrl = $"/images/User/Book/uploads/{fileName}";
        }

        var actor = GetCurrentActor();
        var now = DateTime.UtcNow;

        var book = new Book
        {
            BookCode = request.BookCode.Trim(),
            Title = request.BookTitle.Trim(),
            Author = request.Author.Trim(),
            CategoryName = request.CategoryName.Trim(),
            Isbn = NormalizeOptionalText(request.Isbn),
            Quantity = request.Quantity.Value,
            Pages = request.Pages.Value,
            Year = request.Year.Value,
            Status = normalizedStatus,
            Description = NormalizeOptionalText(request.Description),
            ImageUrl = imageUrl,
            Rating = Math.Clamp(request.Rating ?? 5, 0, 5),
            CreatedBy = actor,
            CreatedDate = now
        };

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Name == book.CategoryName);
        if (!categoryExists)
        {
            _context.Categories.Add(new Category
            {
                Name = book.CategoryName,
                CreatedBy = actor,
                CreatedDate = now
            });
        }

        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Book added successfully." });
    }

    [HttpPost("admin/managebooks/delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound(new { success = false, message = "Book not found." });
        }

        _context.Books.Remove(book);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Book deleted successfully." });
    }

    [HttpPost("admin/managebooks/update/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateBookRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BookCode) ||
            string.IsNullOrWhiteSpace(request.BookTitle) ||
            string.IsNullOrWhiteSpace(request.Author) ||
            string.IsNullOrWhiteSpace(request.CategoryName) ||
            !request.Quantity.HasValue ||
            request.Quantity.Value < 0 ||
            !request.Pages.HasValue ||
            request.Pages.Value <= 0 ||
            !request.Year.HasValue ||
            !IsValidYear(request.Year.Value))
        {
            return BadRequest(new { success = false, message = "Please fill all required fields correctly." });
        }

        var normalizedStatus = NormalizeStatus(request.Status);
        if (string.IsNullOrWhiteSpace(normalizedStatus))
        {
            return BadRequest(new { success = false, message = "Please select a valid status." });
        }

        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound(new { success = false, message = "Book not found." });
        }

        book.BookCode = request.BookCode.Trim();
        book.Title = request.BookTitle.Trim();
        book.Author = request.Author.Trim();
        book.CategoryName = request.CategoryName.Trim();
        book.Isbn = NormalizeOptionalText(request.Isbn);
        book.Quantity = request.Quantity.Value;
        book.Pages = request.Pages.Value;
        book.Year = request.Year.Value;
        book.Status = normalizedStatus;
        book.Description = NormalizeOptionalText(request.Description);

        if (request.Rating.HasValue)
        {
            book.Rating = Math.Clamp(request.Rating.Value, 0, 5);
        }

        if (request.BookImage is { Length: > 0 })
        {
            var uploadsDirectory = Path.Combine(_environment.WebRootPath, "images", "User", "Book", "uploads");
            Directory.CreateDirectory(uploadsDirectory);

            var extension = Path.GetExtension(request.BookImage.FileName);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsDirectory, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await request.BookImage.CopyToAsync(stream);

            book.ImageUrl = $"/images/User/Book/uploads/{fileName}";
        }

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Name == book.CategoryName);
        if (!categoryExists)
        {
            _context.Categories.Add(new Category
            {
                Name = book.CategoryName,
                CreatedBy = GetCurrentActor(),
                CreatedDate = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Book updated successfully." });
    }

    public sealed class AddBookRequest
    {
        public string BookCode { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? Isbn { get; set; }
        public int? Quantity { get; set; }
        public int? Pages { get; set; }
        public int? Year { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? Rating { get; set; }
        public IFormFile? BookImage { get; set; }
    }

    public sealed class UpdateBookRequest
    {
        public string BookCode { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? Isbn { get; set; }
        public int? Quantity { get; set; }
        public int? Pages { get; set; }
        public int? Year { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? Rating { get; set; }
        public IFormFile? BookImage { get; set; }
    }

    private static bool IsValidYear(int year)
    {
        return year is >= 1000 and <= 9999;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string NormalizeStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return AllowedStatuses.Contains(normalized) ? normalized : string.Empty;
    }

    private string GetCurrentActor()
    {
        var actor = User?.Identity?.Name?.Trim();
        return string.IsNullOrWhiteSpace(actor) ? "System Admin" : actor;
    }
}
