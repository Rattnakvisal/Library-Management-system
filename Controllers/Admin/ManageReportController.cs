using Library_Management_system.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_system.Controllers.Admin;

[Authorize(Roles = "Admin")]
public class ManageReportController : Controller
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;
    private const decimal FinePerLateDay = 1.00m;

    private static readonly HashSet<string> AllowedReportTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "borrowing",
        "returns",
        "most-borrowed",
        "fine-collection"
    };

    private readonly ApplicationDbContext _context;

    public ManageReportController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("admin/managereport")]
    public IActionResult Index()
    {
        return View("~/Views/Admin/ManageReport/Index.cshtml");
    }

    [HttpGet("admin/managereport/data")]
    public async Task<IActionResult> Data(
        string? reportType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = DefaultPageSize)
    {
        var normalizedType = NormalizeReportType(reportType);
        var from = fromDate?.Date;
        var to = toDate?.Date;

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            return BadRequest(new { success = false, message = "From date cannot be after to date." });
        }

        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var result = normalizedType switch
        {
            "returns" => await BuildReturnsReportAsync(from, to, safePage, safePageSize),
            "most-borrowed" => await BuildMostBorrowedReportAsync(from, to, safePage, safePageSize),
            "fine-collection" => await BuildFineCollectionReportAsync(from, to, safePage, safePageSize),
            _ => await BuildBorrowingReportAsync(from, to, safePage, safePageSize)
        };

        return Ok(new
        {
            success = true,
            reportType = normalizedType,
            fromDate = from?.ToString("yyyy-MM-dd"),
            toDate = to?.ToString("yyyy-MM-dd"),
            page = result.Page,
            pageSize = result.PageSize,
            totalRows = result.TotalRows,
            totalPages = result.TotalPages,
            rows = result.Rows
        });
    }

    private async Task<ReportPageResult> BuildBorrowingReportAsync(DateTime? from, DateTime? to, int page, int pageSize)
    {
        var utcToday = DateTime.UtcNow.Date;
        var fromBoundary = from;
        var toExclusive = to?.AddDays(1);

        var query = _context.BorrowingRecords
            .AsNoTracking()
            .Include(x => x.Book)
            .AsQueryable();

        if (fromBoundary.HasValue)
        {
            query = query.Where(x => x.BorrowDate >= fromBoundary.Value);
        }

        if (toExclusive.HasValue)
        {
            query = query.Where(x => x.BorrowDate < toExclusive.Value);
        }

        var totalRows = await query.CountAsync();
        var (safePage, totalPages) = NormalizePage(page, pageSize, totalRows);

        var rowsRaw = await query
            .OrderByDescending(x => x.BorrowDate)
            .ThenByDescending(x => x.Id)
            .Skip((safePage - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                Title = x.Book != null ? x.Book.Title : "(Missing book)",
                x.BorrowDate,
                x.DueDate,
                x.Status,
                x.ReturnDate
            })
            .ToListAsync();

        var rows = rowsRaw
            .Select(x => (object)new
            {
                id = x.Id,
                title = x.Title,
                borrowDate = x.BorrowDate.ToString("yyyy-MM-dd"),
                dueDate = x.DueDate.ToString("yyyy-MM-dd"),
                status = ToDisplayStatus(ComputeBorrowingStatus(x.Status, x.DueDate, x.ReturnDate, utcToday))
            })
            .ToList();

        return new ReportPageResult(safePage, pageSize, totalRows, totalPages, rows);
    }

    private async Task<ReportPageResult> BuildReturnsReportAsync(DateTime? from, DateTime? to, int page, int pageSize)
    {
        var fromBoundary = from;
        var toExclusive = to?.AddDays(1);

        var query = _context.BorrowingRecords
            .AsNoTracking()
            .Include(x => x.Book)
            .Where(x => x.ReturnDate.HasValue || x.Status == "returned");

        if (fromBoundary.HasValue)
        {
            query = query.Where(x => (x.ReturnDate ?? x.DueDate) >= fromBoundary.Value);
        }

        if (toExclusive.HasValue)
        {
            query = query.Where(x => (x.ReturnDate ?? x.DueDate) < toExclusive.Value);
        }

        var totalRows = await query.CountAsync();
        var (safePage, totalPages) = NormalizePage(page, pageSize, totalRows);

        var rowsRaw = await query
            .OrderByDescending(x => x.ReturnDate ?? x.DueDate)
            .ThenByDescending(x => x.Id)
            .Skip((safePage - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                Title = x.Book != null ? x.Book.Title : "(Missing book)",
                x.Username,
                ReturnDate = x.ReturnDate ?? x.DueDate
            })
            .ToListAsync();

        var rows = rowsRaw
            .Select(x => (object)new
            {
                id = $"R-{x.Id:0000}",
                title = x.Title,
                user = x.Username,
                returnDate = x.ReturnDate.ToString("yyyy-MM-dd"),
                status = "Returned"
            })
            .ToList();

        return new ReportPageResult(safePage, pageSize, totalRows, totalPages, rows);
    }

    private async Task<ReportPageResult> BuildMostBorrowedReportAsync(DateTime? from, DateTime? to, int page, int pageSize)
    {
        var fromBoundary = from;
        var toExclusive = to?.AddDays(1);

        var borrowings = await _context.BorrowingRecords
            .AsNoTracking()
            .Include(x => x.Book)
            .Where(x =>
                (!fromBoundary.HasValue || x.BorrowDate >= fromBoundary.Value) &&
                (!toExclusive.HasValue || x.BorrowDate < toExclusive.Value))
            .ToListAsync();

        var grouped = borrowings
            .GroupBy(x => new
            {
                x.BookId,
                Title = string.IsNullOrWhiteSpace(x.Book?.Title) ? "(Missing book)" : x.Book!.Title,
                Category = string.IsNullOrWhiteSpace(x.Book?.CategoryName) ? "Uncategorized" : x.Book!.CategoryName
            })
            .Select(g => new
            {
                g.Key.Title,
                g.Key.Category,
                TotalBorrowed = g.Count()
            })
            .OrderByDescending(x => x.TotalBorrowed)
            .ThenBy(x => x.Title)
            .ToList();

        var totalRows = grouped.Count;
        var (safePage, totalPages) = NormalizePage(page, pageSize, totalRows);

        var pageRows = grouped
            .Skip((safePage - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var rankStart = (safePage - 1) * pageSize + 1;
        var rows = pageRows
            .Select((x, index) => (object)new
            {
                rank = rankStart + index,
                title = x.Title,
                category = x.Category,
                total = x.TotalBorrowed,
                status = "Borrowed"
            })
            .ToList();

        return new ReportPageResult(safePage, pageSize, totalRows, totalPages, rows);
    }

    private async Task<ReportPageResult> BuildFineCollectionReportAsync(DateTime? from, DateTime? to, int page, int pageSize)
    {
        var utcToday = DateTime.UtcNow.Date;

        var borrowings = await _context.BorrowingRecords
            .AsNoTracking()
            .Include(x => x.Book)
            .ToListAsync();

        var rowsRaw = borrowings
            .Select(x =>
            {
                var status = ComputeBorrowingStatus(x.Status, x.DueDate, x.ReturnDate, utcToday);
                var lateDays = CalculateLateDays(x.DueDate, x.ReturnDate, utcToday, status);
                if (lateDays <= 0)
                {
                    return null;
                }

                var paid = string.Equals(status, "returned", StringComparison.OrdinalIgnoreCase);
                var fineDate = (paid ? x.ReturnDate : x.DueDate).GetValueOrDefault(x.DueDate).Date;
                if (from.HasValue && fineDate < from.Value)
                {
                    return null;
                }

                if (to.HasValue && fineDate > to.Value)
                {
                    return null;
                }

                var amount = lateDays * FinePerLateDay;
                return new
                {
                    x.Id,
                    Title = x.Book?.Title ?? "(Missing book)",
                    x.Username,
                    Amount = amount,
                    Paid = paid,
                    PaidDate = paid ? x.ReturnDate?.Date : null,
                    FineDate = fineDate
                };
            })
            .Where(x => x != null)
            .OrderByDescending(x => x!.FineDate)
            .ThenByDescending(x => x!.Id)
            .ToList();

        var totalRows = rowsRaw.Count;
        var (safePage, totalPages) = NormalizePage(page, pageSize, totalRows);

        var pageRows = rowsRaw
            .Skip((safePage - 1) * pageSize)
            .Take(pageSize)
            .Select(x => (object)new
            {
                id = $"F-{x!.Id:0000}",
                title = x.Title,
                user = x.Username,
                amount = x.Amount,
                paid = x.Paid ? "Paid" : "Unpaid",
                paidDate = x.PaidDate?.ToString("yyyy-MM-dd") ?? string.Empty
            })
            .ToList();

        return new ReportPageResult(safePage, pageSize, totalRows, totalPages, pageRows);
    }

    private static string NormalizeReportType(string? reportType)
    {
        if (string.IsNullOrWhiteSpace(reportType))
        {
            return "borrowing";
        }

        var normalized = reportType.Trim().ToLowerInvariant();
        return AllowedReportTypes.Contains(normalized) ? normalized : "borrowing";
    }

    private static (int SafePage, int TotalPages) NormalizePage(int requestedPage, int pageSize, int totalRows)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalRows / (double)pageSize));
        var safePage = Math.Clamp(requestedPage, 1, totalPages);
        return (safePage, totalPages);
    }

    private static string ComputeBorrowingStatus(string? currentStatus, DateTime dueDate, DateTime? returnDate, DateTime utcToday)
    {
        if (returnDate.HasValue || string.Equals(currentStatus, "returned", StringComparison.OrdinalIgnoreCase))
        {
            return "returned";
        }

        return dueDate.Date < utcToday ? "overdue" : "active";
    }

    private static int CalculateLateDays(DateTime dueDate, DateTime? returnDate, DateTime utcToday, string status)
    {
        if (string.Equals(status, "returned", StringComparison.OrdinalIgnoreCase) && returnDate.HasValue)
        {
            return Math.Max(0, (returnDate.Value.Date - dueDate.Date).Days);
        }

        if (string.Equals(status, "overdue", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Max(0, (utcToday - dueDate.Date).Days);
        }

        return 0;
    }

    private static string ToDisplayStatus(string status)
    {
        return status switch
        {
            "active" => "Borrowed",
            "overdue" => "Overdue",
            "returned" => "Returned",
            _ => "Borrowed"
        };
    }

    private sealed record ReportPageResult(
        int Page,
        int PageSize,
        int TotalRows,
        int TotalPages,
        IReadOnlyList<object> Rows);
}
