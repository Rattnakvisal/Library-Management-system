using Library_Management_system.Data;
using Library_Management_system.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_system.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("admin/manageauthor")]
public class ManageAuthorController : Controller
{
    private readonly ApplicationDbContext _context;

    public ManageAuthorController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromForm] CreateAuthorRequest request)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { success = false, message = "Author name is required." });
        }

        var exists = await _context.Authors.AnyAsync(a => a.AuthorName == name);
        if (exists)
        {
            return BadRequest(new { success = false, message = "Author already exists." });
        }

        var author = new Author
        {
            AuthorName = name,
            CreatedBy = GetCurrentActor(),
            CreatedDate = DateTime.UtcNow
        };

        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Author added successfully.", authorId = author.AuthorID });
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update([FromForm] UpdateAuthorRequest request)
    {
        var name = request.Name?.Trim();
        if (request.AuthorId <= 0 || string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { success = false, message = "Author id and name are required." });
        }

        var author = await _context.Authors.FirstOrDefaultAsync(a => a.AuthorID == request.AuthorId);
        if (author == null)
        {
            return NotFound(new { success = false, message = "Author not found." });
        }

        var duplicate = await _context.Authors.AnyAsync(a => a.AuthorID != request.AuthorId && a.AuthorName == name);
        if (duplicate)
        {
            return BadRequest(new { success = false, message = "Author name already exists." });
        }

        var oldName = author.AuthorName;
        author.AuthorName = name;

        var books = await _context.Books
            .Where(b => b.AuthorId == author.AuthorID || b.Author == oldName)
            .ToListAsync();

        foreach (var book in books)
        {
            book.AuthorId = author.AuthorID;
            book.Author = name;
            book.AuthorEntity = author;
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Author updated successfully." });
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteAuthorRequest request)
    {
        if (request.AuthorId <= 0)
        {
            return BadRequest(new { success = false, message = "Author id is required." });
        }

        var author = await _context.Authors.FirstOrDefaultAsync(a => a.AuthorID == request.AuthorId);
        if (author == null)
        {
            return NotFound(new { success = false, message = "Author not found." });
        }

        const string fallbackAuthorName = "Unknown Author";
        var fallbackAuthor = await _context.Authors
            .FirstOrDefaultAsync(a => a.AuthorName == fallbackAuthorName);

        if (fallbackAuthor == null)
        {
            fallbackAuthor = new Author
            {
                AuthorName = fallbackAuthorName,
                CreatedBy = GetCurrentActor(),
                CreatedDate = DateTime.UtcNow
            };

            _context.Authors.Add(fallbackAuthor);
            await _context.SaveChangesAsync();
        }

        if (author.AuthorID == fallbackAuthor.AuthorID)
        {
            return BadRequest(new { success = false, message = "Default author cannot be deleted." });
        }

        var affectedBooks = await _context.Books
            .Where(b => b.AuthorId == author.AuthorID || b.Author == author.AuthorName)
            .ToListAsync();

        foreach (var book in affectedBooks)
        {
            book.AuthorId = fallbackAuthor.AuthorID;
            book.Author = fallbackAuthor.AuthorName;
            book.AuthorEntity = fallbackAuthor;
        }

        _context.Authors.Remove(author);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Author deleted. Books moved to Unknown Author.",
            updatedBooks = affectedBooks.Count
        });
    }

    private string GetCurrentActor()
    {
        var actor = User?.Identity?.Name?.Trim();
        return string.IsNullOrWhiteSpace(actor) ? "System Admin" : actor;
    }

    public sealed class CreateAuthorRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class UpdateAuthorRequest
    {
        public int AuthorId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public sealed class DeleteAuthorRequest
    {
        public int AuthorId { get; set; }
    }
}
