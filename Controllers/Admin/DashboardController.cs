using Library_Management_system.Data;
using Library_Management_system.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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

        [HttpGet("")]
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
                .ToListAsync();

            var borrowings = await _context.BorrowingRecords
                .AsNoTracking()
                .Include(br => br.Book)
                .ToListAsync();

            var borrowingSnapshots = borrowings
                .Select(br =>
                {
                    var status = ComputeBorrowingStatus(br.Status, br.DueDate, br.ReturnDate, utcToday);
                    var lateDays = CalculateLateDays(br.DueDate, utcToday, status);
                    return new
                    {
                        Record = br,
                        Status = status,
                        LateDays = lateDays,
                        Fine = lateDays * FinePerLateDay
                    };
                })
                .ToList();

            var borrowedBooks = borrowingSnapshots.Count(x => x.Status != "returned");
            var totalFines = borrowingSnapshots.Sum(x => x.Fine);

            var overdueBorrowings = borrowingSnapshots
                .Where(x => x.Status == "overdue")
                .OrderByDescending(x => x.LateDays)
                .ThenBy(x => x.Record.DueDate)
                .Take(QuickActionsLimit)
                .Select(x => new DashboardOverdueBorrowingItemViewModel
                {
                    BorrowingId = x.Record.Id,
                    BookTitle = x.Record.Book?.Title ?? "(Missing book)",
                    Borrower = string.IsNullOrWhiteSpace(x.Record.Username) ? "Unknown" : x.Record.Username,
                    DueDate = x.Record.DueDate,
                    DaysOverdue = x.LateDays,
                    Fine = x.Fine
                })
                .ToList();

            var recentBorrowings = borrowingSnapshots
                .OrderByDescending(x => x.Record.BorrowDate)
                .ThenByDescending(x => x.Record.Id)
                .Take(QuickActionsLimit)
                .Select(x => new DashboardRecentBorrowingItemViewModel
                {
                    BorrowingId = x.Record.Id,
                    Username = string.IsNullOrWhiteSpace(x.Record.Username) ? "Unknown" : x.Record.Username,
                    BookTitle = x.Record.Book?.Title ?? "(Missing book)",
                    BorrowDate = x.Record.BorrowDate,
                    DueDate = x.Record.DueDate,
                    Status = x.Status
                })
                .ToList();

            var trendStartMonth = new DateTime(utcToday.Year, utcToday.Month, 1).AddMonths(-11);
            var trendBuckets = borrowingSnapshots
                .Where(x => x.Record.BorrowDate.Date >= trendStartMonth)
                .GroupBy(x => new { x.Record.BorrowDate.Year, x.Record.BorrowDate.Month })
                .ToDictionary(
                    g => (g.Key.Year, g.Key.Month),
                    g => g.Count());

            var borrowingTrends = Enumerable.Range(0, 12)
                .Select(offset =>
                {
                    var monthDate = trendStartMonth.AddMonths(offset);
                    return new DashboardBorrowingTrendItemViewModel
                    {
                        Label = monthDate.ToString("MMM yy", CultureInfo.InvariantCulture),
                        Count = trendBuckets.TryGetValue((monthDate.Year, monthDate.Month), out var count) ? count : 0
                    };
                })
                .ToList();

            var model = new DashboardViewModel
            {
                TotalBooks = totalBooks,
                BorrowedBooks = borrowedBooks,
                TotalUsers = totalUsers,
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
