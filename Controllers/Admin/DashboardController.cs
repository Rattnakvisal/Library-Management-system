using Library_Management_system.Data;
using Library_Management_system.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_system.Controllers.Admin
{
    [Authorize(Roles = "Admin,Librarian")]
    [Route("admin/dashboard")]
    public class DashboardController : Controller
    {
        private const int QuickActionsLimit = 5;
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var totalBooks = await _context.Books.CountAsync();
            var totalUsers = await _context.Users.CountAsync();

            var latestUsers = await _context.Users
                .AsNoTracking()
                .OrderByDescending(u => u.CreatedDate ?? DateTime.MinValue)
                .ThenByDescending(u => u.Id)
                .Take(QuickActionsLimit)
                .ToListAsync();

            var userIds = latestUsers.Select(u => u.Id).ToList();
            var genderClaims = await _context.UserClaims
                .AsNoTracking()
                .Where(c => userIds.Contains(c.UserId) && c.ClaimType == "Gender")
                .ToListAsync();

            var genderByUser = genderClaims
                .GroupBy(c => c.UserId)
                .ToDictionary(g => g.Key, g => NormalizeGender(g.Last().ClaimValue));

            var newMembers = latestUsers
                .Select(u => new DashboardNewMemberItemViewModel
                {
                    FullName = string.IsNullOrWhiteSpace(u.FullName) ? u.UserName ?? "Unknown" : u.FullName,
                    Gender = genderByUser.TryGetValue(u.Id, out var gender) ? gender : "Unspecified",
                    PhoneNumber = u.PhoneNumber ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    CreatedDate = u.CreatedDate
                })
                .ToList();

            var newBooks = await _context.Books
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedDate ?? DateTime.MinValue)
                .ThenByDescending(b => b.Id)
                .Take(QuickActionsLimit)
                .Select(b => new DashboardNewBookItemViewModel
                {
                    Title = b.Title,
                    Author = b.Author,
                    CategoryName = b.CategoryName,
                    ImageUrl = b.ImageUrl,
                    CreatedDate = b.CreatedDate
                })
                .ToListAsync();

            var categoryDistribution = await _context.Books
                .AsNoTracking()
                .Where(b => !string.IsNullOrWhiteSpace(b.CategoryName))
                .GroupBy(b => b.CategoryName)
                .Select(g => new DashboardCategoryChartItemViewModel
                {
                    CategoryName = g.Key,
                    BookCount = g.Count()
                })
                .OrderByDescending(x => x.BookCount)
                .ThenBy(x => x.CategoryName)
                .ToListAsync();

            var model = new DashboardViewModel
            {
                TotalBooks = totalBooks,
                TotalUsers = totalUsers,
                BorrowedBooks = 0,
                TotalFines = 0m,
                CategoryDistribution = categoryDistribution,
                NewMembers = newMembers,
                NewBooks = newBooks
            };

            return View("~/Views/Admin/Dashboard/Index.cshtml", model);
        }

        private static string NormalizeGender(string? value)
        {
            if (string.Equals(value, "M", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Male", StringComparison.OrdinalIgnoreCase))
            {
                return "Male";
            }

            if (string.Equals(value, "F", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Female", StringComparison.OrdinalIgnoreCase))
            {
                return "Female";
            }

            return "Unspecified";
        }
    }
}
