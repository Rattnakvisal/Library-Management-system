using Library_Management_system.Data;
using Library_Management_system.Models;
using Library_Management_system.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_system.Controllers.Admin;

[Authorize(Roles = "Admin,Librarian")]
[Route("admin/managecategory")]
public class ManageCategoryController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    public ManageCategoryController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q = null)
    {
        var categoriesQuery = _context.Categories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            categoriesQuery = categoriesQuery.Where(c => c.Name.Contains(keyword));
        }

        var categories = await categoriesQuery
            .OrderBy(c => c.Name)
            .Select(c => new ManageCategoryViewModel
            {
                CategoryId = c.Id,
                Name = c.Name,
                BookCount = _context.Books.Count(b => b.CategoryId == c.Id || b.CategoryName == c.Name),
                CreatedBy = c.CreatedBy ?? string.Empty,
                CreatedDate = c.CreatedDate
            })
            .ToListAsync();

        var authors = await _context.Authors
            .AsNoTracking()
            .OrderBy(a => a.AuthorID)
            .ToListAsync();

        ViewBag.TotalCategories = categories.Count;
        ViewBag.TotalBooksInCategories = categories.Sum(c => c.BookCount);
        ViewBag.TotalAuthors = authors.Count;
        ViewBag.Authors = authors;

        return View("~/Views/Admin/ManageCategory/Index.cshtml", categories);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromForm] CreateCategoryRequest request)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { success = false, message = "Category name is required." });
        }

        var exists = await _context.Categories.AnyAsync(c => c.Name == name);
        if (exists)
        {
            return BadRequest(new { success = false, message = "Category already exists." });
        }

        var imageUrl = await SaveCategoryImageAsync(request.ImageFile);

        _context.Categories.Add(new Category
        {
            Name = name,
            ImageUrl = imageUrl,
            Description = request.Description,
            CreatedBy = GetCurrentActor(),
            CreatedDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Category added successfully." });
    }

    [HttpPost("create-author")]
    public async Task<IActionResult> CreateAuthor([FromForm] CreateAuthorRequest request)
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

        _context.Authors.Add(new Author
        {
            AuthorName = name,
            CreatedBy = GetCurrentActor(),
            CreatedDate = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Author added successfully." });
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update([FromForm] UpdateCategoryRequest request)
    {
        var oldName = request.OldName?.Trim();
        var newName = request.NewName?.Trim();

        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
        {
            return BadRequest(new { success = false, message = "Category names are required." });
        }

        var targetCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Name == oldName);
        if (targetCategory == null)
        {
            return NotFound(new { success = false, message = "Category not found." });
        }

        // Update name if changed
        if (!string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
        {
            var duplicate = await _context.Categories.AnyAsync(c => c.Name == newName);
            if (duplicate)
            {
                return BadRequest(new { success = false, message = "Category name already exists." });
            }

            var books = await _context.Books
                .Where(b => b.CategoryId == targetCategory.Id || b.CategoryName == oldName)
                .ToListAsync();
            foreach (var book in books)
            {
                book.CategoryName = newName;
                book.CategoryId = targetCategory.Id;
            }
            targetCategory.Name = newName;
        }

        // Update image if provided
        if (request.ImageFile != null && request.ImageFile.Length > 0)
        {
            var imageUrl = await SaveCategoryImageAsync(request.ImageFile);
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                targetCategory.ImageUrl = imageUrl;
            }
        }

        // Update description if provided
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            targetCategory.Description = request.Description;
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Category updated successfully." });
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteCategoryRequest request)
    {
        var categoryName = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return BadRequest(new { success = false, message = "Category name is required." });
        }

        var targetCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
        if (targetCategory == null)
        {
            return NotFound(new { success = false, message = "Category not found." });
        }

        var uncategorizedName = "Uncategorized";
        var uncategorizedCategory = await _context.Categories
            .FirstOrDefaultAsync(c => c.Name == uncategorizedName);

        if (uncategorizedCategory == null)
        {
            uncategorizedCategory = new Category
            {
                Name = uncategorizedName,
                CreatedBy = GetCurrentActor(),
                CreatedDate = DateTime.UtcNow
            };

            _context.Categories.Add(uncategorizedCategory);
            await _context.SaveChangesAsync();
        }

        var books = await _context.Books
            .Where(b => b.CategoryId == targetCategory.Id || b.CategoryName == categoryName)
            .ToListAsync();
        foreach (var book in books)
        {
            book.CategoryName = uncategorizedName;
            book.CategoryId = uncategorizedCategory.Id;
        }

        _context.Categories.Remove(targetCategory);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Category deleted. Books moved to Uncategorized." });
    }

    private async Task<string?> SaveCategoryImageAsync(IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
            return null;

        const long maxSizeBytes = 5 * 1024 * 1024;
        if (imageFile.Length > maxSizeBytes)
            return null;

        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            return null;

        if (!imageFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return null;

        var uploadsDirectory = Path.Combine(_environment.WebRootPath, "images", "Admin", "Categories");
        Directory.CreateDirectory(uploadsDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsDirectory, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await imageFile.CopyToAsync(stream);

        return $"/images/Admin/Categories/{fileName}";
    }

    private string GetCurrentActor()
    {
        var actor = User?.Identity?.Name?.Trim();
        return string.IsNullOrWhiteSpace(actor) ? "System Admin" : actor;
    }

    public sealed class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public IFormFile? ImageFile { get; set; }
        public string? Description { get; set; }
    }

    public sealed class UpdateCategoryRequest
    {
        public string OldName { get; set; } = string.Empty;
        public string NewName { get; set; } = string.Empty;
        public IFormFile? ImageFile { get; set; }
        public string? Description { get; set; }
    }

    public sealed class CreateAuthorRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class DeleteCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
    }
}
