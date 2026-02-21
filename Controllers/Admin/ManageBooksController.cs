using Library_Management_system.Data;
using Library_Management_system.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_system.Controllers.Admin;

public class ManageBooksController : Controller
{
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
                b.CategoryName.Contains(keyword));
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
        ViewBag.AvailableCopies = books.Count;
        ViewBag.UnavailableBooks = 0;
        ViewBag.BorrowedBooks = 0;
        ViewBag.BookCategories = categories;

        return View("~/Views/Admin/ManageBooks/Index.cshtml", books);
    }

    [HttpPost("admin/managebooks/add")]
    public async Task<IActionResult> Add([FromForm] AddBookRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BookTitle) ||
            string.IsNullOrWhiteSpace(request.Author) ||
            string.IsNullOrWhiteSpace(request.CategoryName))
        {
            return BadRequest(new { success = false, message = "Book title, author, and category are required." });
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
            Title = request.BookTitle.Trim(),
            Author = request.Author.Trim(),
            CategoryName = request.CategoryName.Trim(),
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
        if (string.IsNullOrWhiteSpace(request.BookTitle) ||
            string.IsNullOrWhiteSpace(request.Author) ||
            string.IsNullOrWhiteSpace(request.CategoryName))
        {
            return BadRequest(new { success = false, message = "Book title, author, and category are required." });
        }

        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound(new { success = false, message = "Book not found." });
        }

        book.Title = request.BookTitle.Trim();
        book.Author = request.Author.Trim();
        book.CategoryName = request.CategoryName.Trim();

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
        public string BookTitle { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int? Rating { get; set; }
        public IFormFile? BookImage { get; set; }
    }

    public sealed class UpdateBookRequest
    {
        public string BookTitle { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int? Rating { get; set; }
        public IFormFile? BookImage { get; set; }
    }

    private string GetCurrentActor()
    {
        var actor = User?.Identity?.Name?.Trim();
        return string.IsNullOrWhiteSpace(actor) ? "System Admin" : actor;
    }
}
