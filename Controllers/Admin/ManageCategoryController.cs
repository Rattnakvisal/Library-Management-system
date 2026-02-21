using Library_Management_system.Data;
using Library_Management_system.Models;
using Library_Management_system.Models.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_system.Controllers.Admin;

public class ManageCategoryController : Controller
{
    private readonly ApplicationDbContext _context;

    public ManageCategoryController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("admin/managecategory")]
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
                Name = c.Name,
                BookCount = _context.Books.Count(b => b.CategoryName == c.Name),
                CreatedBy = c.CreatedBy ?? string.Empty,
                CreatedDate = c.CreatedDate
            })
            .ToListAsync();

        ViewBag.TotalCategories = categories.Count;
        ViewBag.TotalBooksInCategories = categories.Sum(c => c.BookCount);

        return View("~/Views/Admin/ManageCategory/Index.cshtml", categories);
    }

    [HttpPost("admin/managecategory/create")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
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

        _context.Categories.Add(new Category
        {
            Name = name,
            CreatedBy = GetCurrentActor(),
            CreatedDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Category added successfully." });
    }

    [HttpPost("admin/managecategory/rename")]
    public async Task<IActionResult> Rename([FromBody] RenameCategoryRequest request)
    {
        var oldName = request.OldName?.Trim();
        var newName = request.NewName?.Trim();

        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
        {
            return BadRequest(new { success = false, message = "Old and new category names are required." });
        }

        if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "New category name must be different." });
        }

        var targetCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Name == oldName);
        if (targetCategory == null)
        {
            return NotFound(new { success = false, message = "Category not found." });
        }

        var duplicate = await _context.Categories.AnyAsync(c => c.Name == newName);
        if (duplicate)
        {
            return BadRequest(new { success = false, message = "Category name already exists." });
        }

        targetCategory.Name = newName;

        var books = await _context.Books
            .Where(b => b.CategoryName == oldName)
            .ToListAsync();

        foreach (var book in books)
        {
            book.CategoryName = newName;
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Category renamed successfully." });
    }

    [HttpPost("admin/managecategory/delete")]
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
        if (!await _context.Categories.AnyAsync(c => c.Name == uncategorizedName))
        {
            _context.Categories.Add(new Category
            {
                Name = uncategorizedName,
                CreatedBy = GetCurrentActor(),
                CreatedDate = DateTime.UtcNow
            });
        }

        var books = await _context.Books
            .Where(b => b.CategoryName == categoryName)
            .ToListAsync();

        foreach (var book in books)
        {
            book.CategoryName = uncategorizedName;
        }

        _context.Categories.Remove(targetCategory);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Category deleted. Books moved to Uncategorized." });
    }

    public sealed class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class RenameCategoryRequest
    {
        public string OldName { get; set; } = string.Empty;
        public string NewName { get; set; } = string.Empty;
    }

    public sealed class DeleteCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    private string GetCurrentActor()
    {
        var actor = User?.Identity?.Name?.Trim();
        return string.IsNullOrWhiteSpace(actor) ? "System Admin" : actor;
    }
}
