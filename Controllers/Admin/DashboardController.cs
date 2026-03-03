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
        private const decimal FinePerLateDay = 1.00m;
        private const string GenderClaimType = "Gender";
        private const string UserCodeClaimType = "UserCode";
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var totalBooks = await _context.Books.CountAsync();
            var totalUsers = await _context.Users.CountAsync();
            var utcToday = DateTime.UtcNow.Date;
            var utcNowOffset = DateTimeOffset.UtcNow;

            var latestUsers = await _context.Users
                .AsNoTracking()
                .OrderByDescending(u => u.CreatedDate ?? DateTime.MinValue)
                .ThenByDescending(u => u.Id)
                .Take(QuickActionsLimit)
                .ToListAsync();

            var userIds = latestUsers.Select(u => u.Id).ToList();
            var genderClaims = await _context.UserClaims
                .AsNoTracking()
                .Where(c => userIds.Contains(c.UserId) && c.ClaimType == GenderClaimType)
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

            var restrictedMembersCount = await _context.Users
                .AsNoTracking()
                .CountAsync(u => u.LockoutEnabled && u.LockoutEnd.HasValue && u.LockoutEnd > utcNowOffset);

            var restrictedUsers = await _context.Users
                .AsNoTracking()
                .Where(u => u.LockoutEnabled && u.LockoutEnd.HasValue && u.LockoutEnd > utcNowOffset)
                .OrderByDescending(u => u.LockoutEnd)
                .ThenBy(u => u.FullName)
                .ThenBy(u => u.Id)
                .Take(QuickActionsLimit)
                .ToListAsync();

            var restrictedUserIds = restrictedUsers.Select(u => u.Id).ToList();
            var memberIdClaims = await _context.UserClaims
                .AsNoTracking()
                .Where(c => restrictedUserIds.Contains(c.UserId) && c.ClaimType == UserCodeClaimType)
                .ToListAsync();

            var memberIdByUser = memberIdClaims
                .GroupBy(c => c.UserId)
                .ToDictionary(g => g.Key, g => g.Last().ClaimValue ?? string.Empty);

            var restrictedMembers = restrictedUsers
                .Select(u => new DashboardRestrictedMemberItemViewModel
                {
                    MemberId = NormalizeMemberId(memberIdByUser.TryGetValue(u.Id, out var code) ? code : string.Empty, u.Id),
                    FullName = string.IsNullOrWhiteSpace(u.FullName) ? u.UserName ?? "Unknown" : u.FullName,
                    Email = u.Email ?? string.Empty,
                    RestrictedUntilUtc = u.LockoutEnd ?? DateTimeOffset.UtcNow
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

            var borrowingRowsRaw = await _context.BorrowingRecords
                .AsNoTracking()
                .Include(br => br.Book)
                .OrderByDescending(br => br.BorrowDate)
                .ThenByDescending(br => br.Id)
                .ToListAsync();

            var borrowingRows = borrowingRowsRaw
                .Select(br =>
                {
                    var status = ComputeBorrowingStatus(br.Status, br.DueDate, br.ReturnDate, utcToday);
                    var lateDays = CalculateLateDays(br.DueDate, utcToday, status);
                    return new
                    {
                        Row = br,
                        Status = status,
                        LateDays = lateDays
                    };
                })
                .ToList();

            var borrowedBooks = borrowingRows.Count(x => x.Status is "active" or "overdue");
            var overdueBorrowings = borrowingRows
                .Where(x => x.Status == "overdue")
                .OrderByDescending(x => x.LateDays)
                .ThenBy(x => x.Row.DueDate)
                .Take(QuickActionsLimit)
                .Select(x => new DashboardOverdueBorrowingItemViewModel
                {
                    BorrowingId = x.Row.Id,
                    BookTitle = x.Row.Book?.Title ?? "(Missing book)",
                    Borrower = x.Row.Username,
                    DueDate = x.Row.DueDate,
                    DaysOverdue = x.LateDays,
                    Fine = x.LateDays * FinePerLateDay
                })
                .ToList();

            var totalFines = borrowingRows
                .Where(x => x.Status == "overdue")
                .Sum(x => x.LateDays * FinePerLateDay);

            var recentBorrowings = borrowingRows
                .Take(QuickActionsLimit)
                .Select(x => new DashboardRecentBorrowingItemViewModel
                {
                    BorrowingId = x.Row.Id,
                    Username = x.Row.Username,
                    BookTitle = x.Row.Book?.Title ?? "(Missing book)",
                    BorrowDate = x.Row.BorrowDate,
                    DueDate = x.Row.DueDate,
                    Status = x.Status
                })
                .ToList();

            var monthStartUtc = new DateTime(utcToday.Year, utcToday.Month, 1).AddMonths(-11);
            var borrowingsForTrend = borrowingRowsRaw
                .Where(br => br.BorrowDate.Date >= monthStartUtc.Date)
                .ToList();
            var trendLookup = borrowingsForTrend
                .GroupBy(br => new DateTime(br.BorrowDate.Year, br.BorrowDate.Month, 1))
                .ToDictionary(g => g.Key, g => g.Count());

            var borrowingTrends = Enumerable.Range(0, 12)
                .Select(offset =>
                {
                    var month = monthStartUtc.AddMonths(offset);
                    trendLookup.TryGetValue(month, out var count);
                    return new DashboardBorrowingTrendItemViewModel
                    {
                        Label = month.ToString("MMM"),
                        Count = count
                    };
                })
                .ToList();

            var model = new DashboardViewModel
            {
                TotalBooks = totalBooks,
                TotalUsers = totalUsers,
                BorrowedBooks = borrowedBooks,
                TotalFines = totalFines,
                RestrictedMembersCount = restrictedMembersCount,
                OverdueBorrowings = overdueBorrowings,
                RecentBorrowings = recentBorrowings,
                BorrowingTrends = borrowingTrends,
                CategoryDistribution = categoryDistribution,
                NewMembers = newMembers,
                NewBooks = newBooks,
                RestrictedMembers = restrictedMembers
            };

            return View("~/Views/Admin/Dashboard/Index.cshtml", model);
        }

        private static string ComputeBorrowingStatus(string? currentStatus, DateTime dueDate, DateTime? returnDate, DateTime utcToday)
        {
            if (returnDate.HasValue || string.Equals(currentStatus, "returned", StringComparison.OrdinalIgnoreCase))
            {
                return "returned";
            }

            return dueDate.Date < utcToday ? "overdue" : "active";
        }

        private static int CalculateLateDays(DateTime dueDate, DateTime utcToday, string status)
        {
            if (!string.Equals(status, "overdue", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            var delta = (utcToday - dueDate.Date).Days;
            return Math.Max(0, delta);
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

        private static string NormalizeMemberId(string? rawCode, string userId)
        {
            var digits = new string((rawCode ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digits.Length == 7)
            {
                return digits;
            }

            if (digits.Length > 7)
            {
                return digits[^7..];
            }

            if (digits.Length > 0)
            {
                return digits.PadLeft(7, '0');
            }

            var hash = 0;
            foreach (var ch in userId)
            {
                hash = ((hash * 31) + ch) % 10000000;
            }

            if (hash < 0)
            {
                hash *= -1;
            }

            return hash.ToString("D7");
        }
    }
}
