using Library_Management_system.Data;
using Library_Management_system.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_system.Controllers.Admin;

[Authorize(Roles = "Admin,Librarian")]
[Route("admin/manageborrowingbook")]
public class ManageBorrowingBookController : Controller
{
    private const int DefaultBorrowingDays = 7;
    private const int MaxActiveBorrowingsPerUser = 3;
    private const decimal FinePerLateDay = 1.00m;

    private static readonly HashSet<string> BorrowingStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "active",
        "overdue",
        "returned"
    };

    private static readonly HashSet<string> ReservationStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "pending",
        "approved",
        "rejected"
    };

    private readonly ApplicationDbContext _context;

    public ManageBorrowingBookController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? bq = null,
        string? bs = null,
        string? rq = null,
        string? rs = null)
    {
        var borrowingKeyword = (bq ?? string.Empty).Trim();
        var reservationKeyword = (rq ?? string.Empty).Trim();
        var borrowingStatus = NormalizeBorrowingStatus(bs);
        var reservationStatus = NormalizeReservationStatus(rs);

        var borrowingsRaw = await _context.BorrowingRecords
            .AsNoTracking()
            .Include(x => x.Book)
            .OrderByDescending(x => x.BorrowDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        var borrowingRows = borrowingsRaw
            .Select(x =>
            {
                var status = ComputeBorrowingStatus(x.Status, x.DueDate, x.ReturnDate);
                return new BorrowingRowViewModel
                {
                    Id = x.Id,
                    Username = x.Username,
                    BookId = x.BookId,
                    BookCode = string.IsNullOrWhiteSpace(x.Book?.BookCode) ? $"A{x.BookId:0000}" : x.Book!.BookCode,
                    BookTitle = x.Book?.Title ?? "(Missing book)",
                    BorrowDate = x.BorrowDate,
                    DueDate = x.DueDate,
                    Status = status
                };
            })
            .Where(x =>
                (string.IsNullOrWhiteSpace(borrowingKeyword) ||
                 x.Username.Contains(borrowingKeyword, StringComparison.OrdinalIgnoreCase) ||
                 x.BookTitle.Contains(borrowingKeyword, StringComparison.OrdinalIgnoreCase) ||
                 x.BookCode.Contains(borrowingKeyword, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(borrowingStatus) || x.Status == borrowingStatus))
            .ToList();

        var reservationCandidates = await _context.CartItems
            .AsNoTracking()
            .Include(ci => ci.Book)
            .Where(ci => ci.ReservationStatus != "none")
            .OrderByDescending(ci => ci.RequestedDate ?? ci.CreatedDate)
            .ThenByDescending(ci => ci.Id)
            .ToListAsync();

        var userIds = reservationCandidates
            .Select(ci => TryExtractUserId(ci.OwnerKey))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        var usersById = await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => string.IsNullOrWhiteSpace(u.FullName) ? (u.UserName ?? "User") : u.FullName);

        var reservationRows = reservationCandidates
            .Select(ci =>
            {
                var status = NormalizeReservationStatus(ci.ReservationStatus);
                var bookCode = string.IsNullOrWhiteSpace(ci.Book?.BookCode) ? $"A{ci.BookId:0000}" : ci.Book!.BookCode;
                var username = ResolveReservationUsername(ci.OwnerKey, usersById);
                return new ReservationRowViewModel
                {
                    Id = ci.Id,
                    Username = username,
                    BookId = ci.BookId,
                    BookCode = bookCode,
                    BookTitle = ci.Book?.Title ?? "(Missing book)",
                    ReservationDate = (ci.RequestedDate ?? ci.CreatedDate),
                    Status = string.IsNullOrWhiteSpace(status) ? "pending" : status
                };
            })
            .Where(x =>
                (string.IsNullOrWhiteSpace(reservationKeyword) ||
                 x.Username.Contains(reservationKeyword, StringComparison.OrdinalIgnoreCase) ||
                 x.BookTitle.Contains(reservationKeyword, StringComparison.OrdinalIgnoreCase) ||
                 x.BookCode.Contains(reservationKeyword, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(reservationStatus) || x.Status == reservationStatus))
            .ToList();

        var bookOptions = await _context.Books
            .AsNoTracking()
            .OrderBy(b => b.BookCode)
            .ThenBy(b => b.Title)
            .Select(b => new BookOptionViewModel
            {
                BookId = b.Id,
                BookCode = b.BookCode,
                Title = b.Title,
                Quantity = b.Quantity
            })
            .ToListAsync();

        var model = new ManageBorrowingViewModel
        {
            Borrowings = borrowingRows,
            Reservations = reservationRows,
            BookOptions = bookOptions,
            BorrowingQuery = borrowingKeyword,
            BorrowingStatus = borrowingStatus,
            ReservationQuery = reservationKeyword,
            ReservationStatus = reservationStatus
        };

        return View("~/Views/Admin/ManageBorrowingBook/Index.cshtml", model);
    }

    [HttpPost("borrowing/create")]
    public async Task<IActionResult> CreateBorrowing([FromForm] CreateBorrowingRequest request)
    {
        var normalizedUsername = NormalizeUsername(request.Username);
        if (string.IsNullOrWhiteSpace(normalizedUsername) ||
            string.IsNullOrWhiteSpace(request.BookCode))
        {
            return BadRequest(new { success = false, message = "Username and Book Code are required." });
        }

        var userBorrowingState = await GetBorrowingStateAsync(normalizedUsername);
        if (userBorrowingState.OverdueCount > 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "This user has overdue borrowing(s). Resolve overdue books before creating a new borrowing."
            });
        }

        if (userBorrowingState.ActiveCount >= MaxActiveBorrowingsPerUser)
        {
            return BadRequest(new
            {
                success = false,
                message = $"Borrowing limit exceeded. Maximum active borrowings per user is {MaxActiveBorrowingsPerUser}."
            });
        }

        var book = await _context.Books.FirstOrDefaultAsync(b => b.BookCode == request.BookCode.Trim());
        if (book == null)
        {
            return BadRequest(new { success = false, message = "Book code not found." });
        }

        if (book.Quantity <= 0)
        {
            return BadRequest(new { success = false, message = "No available quantity for this book." });
        }

        var reservationPriority = await ValidateReservationPriorityAsync(book.Id, normalizedUsername);
        if (!reservationPriority.Allowed)
        {
            return BadRequest(new { success = false, message = reservationPriority.Message });
        }

        var borrowDate = (request.BorrowDate ?? DateTime.UtcNow).Date;
        var dueDate = borrowDate.AddDays(DefaultBorrowingDays);
        var status = ComputeBorrowingStatus("active", dueDate, null);

        _context.BorrowingRecords.Add(new Models.BorrowingRecord
        {
            Username = normalizedUsername,
            BookId = book.Id,
            BorrowDate = borrowDate,
            DueDate = dueDate,
            Status = status,
            Source = "in_person",
            CreatedBy = User?.Identity?.Name,
            CreatedDate = DateTime.UtcNow
        });

        if (reservationPriority.MatchedReservation is { } matchedReservation)
        {
            matchedReservation.IsRequested = true;
            matchedReservation.ReservationStatus = "approved";
            matchedReservation.RequestedDate ??= DateTime.UtcNow;
            matchedReservation.ReservationUpdatedDate = DateTime.UtcNow;
        }

        book.Quantity -= 1;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Borrowing created successfully." });
    }

    [HttpPost("borrowing/update/{id:int}")]
    public async Task<IActionResult> UpdateBorrowing(int id, [FromForm] UpdateBorrowingRequest request)
    {
        var normalizedUsername = NormalizeUsername(request.Username);
        if (string.IsNullOrWhiteSpace(normalizedUsername) ||
            string.IsNullOrWhiteSpace(request.BookCode))
        {
            return BadRequest(new { success = false, message = "Username and Book Code are required." });
        }

        var borrowing = await _context.BorrowingRecords
            .Include(br => br.Book)
            .FirstOrDefaultAsync(br => br.Id == id);
        if (borrowing == null)
        {
            return NotFound(new { success = false, message = "Borrowing record not found." });
        }

        if (string.Equals(borrowing.Status, "returned", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "Returned borrowing cannot be edited." });
        }

        var userBorrowingState = await GetBorrowingStateAsync(normalizedUsername, borrowing.Id);
        if (userBorrowingState.OverdueCount > 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "This user has overdue borrowing(s). Resolve overdue books before assigning this borrowing."
            });
        }

        if (userBorrowingState.ActiveCount >= MaxActiveBorrowingsPerUser)
        {
            return BadRequest(new
            {
                success = false,
                message = $"Borrowing limit exceeded. Maximum active borrowings per user is {MaxActiveBorrowingsPerUser}."
            });
        }

        var newBook = await _context.Books.FirstOrDefaultAsync(b => b.BookCode == request.BookCode.Trim());
        if (newBook == null)
        {
            return BadRequest(new { success = false, message = "Book code not found." });
        }

        if (newBook.Id != borrowing.BookId && newBook.Quantity <= 0)
        {
            return BadRequest(new { success = false, message = "No available quantity for selected book." });
        }

        if (newBook.Id != borrowing.BookId)
        {
            if (borrowing.Book != null)
            {
                borrowing.Book.Quantity += 1;
            }
            newBook.Quantity -= 1;
            borrowing.BookId = newBook.Id;
        }

        var borrowDate = (request.BorrowDate ?? borrowing.BorrowDate).Date;
        var dueDate = borrowDate.AddDays(DefaultBorrowingDays);
        borrowing.Username = normalizedUsername;
        borrowing.BorrowDate = borrowDate;
        borrowing.DueDate = dueDate;
        borrowing.Status = ComputeBorrowingStatus("active", dueDate, borrowing.ReturnDate);

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Borrowing updated successfully." });
    }

    [HttpPost("borrowing/return/{id:int}")]
    public async Task<IActionResult> ProcessReturn(int id)
    {
        var borrowing = await _context.BorrowingRecords
            .Include(br => br.Book)
            .FirstOrDefaultAsync(br => br.Id == id);
        if (borrowing == null)
        {
            return NotFound(new { success = false, message = "Borrowing record not found." });
        }

        if (string.Equals(borrowing.Status, "returned", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "This borrowing was already returned." });
        }

        var returnedAt = DateTime.UtcNow;
        borrowing.Status = "returned";
        borrowing.ReturnDate = returnedAt;

        if (borrowing.Book != null)
        {
            borrowing.Book.Quantity += 1;
        }

        var lateDays = CalculateLateDays(borrowing.DueDate, returnedAt);
        var fineAmount = lateDays > 0 ? lateDays * FinePerLateDay : 0m;

        var nextPendingReservation = await _context.CartItems
            .Where(ci => ci.BookId == borrowing.BookId && ci.ReservationStatus == "pending")
            .OrderBy(ci => ci.RequestedDate ?? ci.CreatedDate)
            .ThenBy(ci => ci.Id)
            .FirstOrDefaultAsync();

        string message;
        if (lateDays > 0)
        {
            message = $"Book marked as returned. Overdue by {lateDays} day(s). Estimated fine: ${fineAmount:0.00}.";
        }
        else
        {
            message = "Book marked as returned.";
        }

        if (nextPendingReservation != null)
        {
            var ownerNames = await ResolveOwnerDisplayNamesAsync(new[] { nextPendingReservation.OwnerKey });
            var priorityUser = ResolveReservationUsername(nextPendingReservation.OwnerKey, ownerNames);
            nextPendingReservation.ReservationUpdatedDate = DateTime.UtcNow;
            message += $" Reservation priority is now for {priorityUser}.";
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message });
    }

    [HttpPost("reservation/approve/{id:int}")]
    public async Task<IActionResult> ApproveReservation(int id)
    {
        var reservation = await _context.CartItems
            .Include(ci => ci.Book)
            .FirstOrDefaultAsync(ci => ci.Id == id);
        if (reservation == null)
        {
            return NotFound(new { success = false, message = "Reservation not found." });
        }

        var sourceKey = BuildReservationBorrowingSource(id);
        var linkedBorrowingExists = await _context.BorrowingRecords
            .AsNoTracking()
            .AnyAsync(br => br.Source == sourceKey);

        var currentStatus = NormalizeReservationStatus(reservation.ReservationStatus);
        if (currentStatus == "approved")
        {
            if (linkedBorrowingExists)
            {
                return Ok(new { success = true, message = "Reservation already approved." });
            }

            var borrowerNameForSync = await ResolveBorrowerNameForReservationAsync(reservation.OwnerKey);
            var syncBorrowDate = DateTime.UtcNow;
            var syncDueDate = syncBorrowDate.AddDays(DefaultBorrowingDays);

            _context.BorrowingRecords.Add(new Models.BorrowingRecord
            {
                Username = borrowerNameForSync,
                BookId = reservation.BookId,
                BorrowDate = syncBorrowDate,
                DueDate = syncDueDate,
                Status = ComputeBorrowingStatus("active", syncDueDate, null),
                Source = sourceKey,
                CreatedBy = User?.Identity?.Name,
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Reservation already approved. Borrowing record has been synced." });
        }

        if (currentStatus != "pending")
        {
            return BadRequest(new { success = false, message = "Only pending reservations can be approved." });
        }

        if (reservation.Book == null)
        {
            return BadRequest(new { success = false, message = "Book not found for reservation." });
        }

        var reservationOrderDate = reservation.RequestedDate ?? reservation.CreatedDate;
        var hasEarlierPendingReservation = await _context.CartItems
            .AsNoTracking()
            .Where(ci => ci.BookId == reservation.BookId &&
                         ci.ReservationStatus == "pending" &&
                         ci.Id != reservation.Id)
            .AnyAsync(ci =>
                (ci.RequestedDate ?? ci.CreatedDate) < reservationOrderDate ||
                ((ci.RequestedDate ?? ci.CreatedDate) == reservationOrderDate && ci.Id < reservation.Id));

        if (hasEarlierPendingReservation)
        {
            return BadRequest(new
            {
                success = false,
                message = "This reservation cannot be approved yet. FIFO rule: approve the earliest pending reservation first."
            });
        }

        var borrowerName = await ResolveBorrowerNameForReservationAsync(reservation.OwnerKey);
        var userBorrowingState = await GetBorrowingStateAsync(borrowerName);
        if (userBorrowingState.OverdueCount > 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "This user has overdue borrowing(s). Cannot approve reservation until overdue books are returned."
            });
        }

        if (userBorrowingState.ActiveCount >= MaxActiveBorrowingsPerUser)
        {
            return BadRequest(new
            {
                success = false,
                message = $"Borrowing limit exceeded. Maximum active borrowings per user is {MaxActiveBorrowingsPerUser}."
            });
        }

        if (reservation.Book.Quantity <= 0)
        {
            return BadRequest(new { success = false, message = "Cannot approve reservation. Book quantity is 0." });
        }

        var borrowDate = DateTime.UtcNow;
        var dueDate = borrowDate.AddDays(DefaultBorrowingDays);
        if (!linkedBorrowingExists)
        {
            _context.BorrowingRecords.Add(new Models.BorrowingRecord
            {
                Username = borrowerName,
                BookId = reservation.BookId,
                BorrowDate = borrowDate,
                DueDate = dueDate,
                Status = ComputeBorrowingStatus("active", dueDate, null),
                Source = sourceKey,
                CreatedBy = User?.Identity?.Name,
                CreatedDate = DateTime.UtcNow
            });
        }

        reservation.Book.Quantity -= 1;
        reservation.IsRequested = true;
        reservation.ReservationStatus = "approved";
        reservation.ReservationUpdatedDate = DateTime.UtcNow;
        reservation.RequestedDate ??= DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Reservation approved and stored in borrowing with a 1-week due date." });
    }

    [HttpPost("reservation/reject/{id:int}")]
    public async Task<IActionResult> RejectReservation(int id)
    {
        var reservation = await _context.CartItems
            .Include(ci => ci.Book)
            .FirstOrDefaultAsync(ci => ci.Id == id);
        if (reservation == null)
        {
            return NotFound(new { success = false, message = "Reservation not found." });
        }

        var currentStatus = NormalizeReservationStatus(reservation.ReservationStatus);
        if (currentStatus == "approved" && reservation.Book != null)
        {
            reservation.Book.Quantity += 1;
        }

        reservation.IsRequested = false;
        reservation.ReservationStatus = "rejected";
        reservation.ReservationUpdatedDate = DateTime.UtcNow;
        reservation.RequestedDate ??= DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Reservation rejected." });
    }

    [HttpPost("reservation/update/{id:int}")]
    public async Task<IActionResult> UpdateReservation(int id, [FromForm] UpdateReservationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BookCode))
        {
            return BadRequest(new { success = false, message = "Book Code is required." });
        }

        var reservation = await _context.CartItems
            .Include(ci => ci.Book)
            .FirstOrDefaultAsync(ci => ci.Id == id);
        if (reservation == null)
        {
            return NotFound(new { success = false, message = "Reservation not found." });
        }

        var newBook = await _context.Books.FirstOrDefaultAsync(b => b.BookCode == request.BookCode.Trim());
        if (newBook == null)
        {
            return BadRequest(new { success = false, message = "Book code not found." });
        }

        var currentStatus = NormalizeReservationStatus(reservation.ReservationStatus);
        if (newBook.Id != reservation.BookId)
        {
            if (currentStatus == "approved")
            {
                if (newBook.Quantity <= 0)
                {
                    return BadRequest(new { success = false, message = "No available quantity for selected book." });
                }

                if (reservation.Book != null)
                {
                    reservation.Book.Quantity += 1;
                }
                newBook.Quantity -= 1;
            }

            reservation.BookId = newBook.Id;
        }

        reservation.RequestedDate = (request.ReservationDate ?? reservation.RequestedDate ?? reservation.CreatedDate).Date;
        reservation.ReservationUpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Reservation updated." });
    }

    private static string ComputeBorrowingStatus(string? currentStatus, DateTime dueDate, DateTime? returnDate)
    {
        if (returnDate.HasValue || string.Equals(currentStatus, "returned", StringComparison.OrdinalIgnoreCase))
        {
            return "returned";
        }

        return dueDate.Date < DateTime.UtcNow.Date ? "overdue" : "active";
    }

    private async Task<(int ActiveCount, int OverdueCount)> GetBorrowingStateAsync(string username, int? excludeBorrowingId = null)
    {
        var query = _context.BorrowingRecords
            .AsNoTracking()
            .Where(br => br.Username == username && br.Status != "returned");

        if (excludeBorrowingId.HasValue)
        {
            query = query.Where(br => br.Id != excludeBorrowingId.Value);
        }

        var activeBorrowings = await query.ToListAsync();
        var overdueCount = activeBorrowings.Count(br =>
            ComputeBorrowingStatus(br.Status, br.DueDate, br.ReturnDate) == "overdue");

        return (activeBorrowings.Count, overdueCount);
    }

    private async Task<ReservationPriorityResult> ValidateReservationPriorityAsync(int bookId, string username)
    {
        var pendingReservations = await _context.CartItems
            .Where(ci => ci.BookId == bookId && ci.ReservationStatus == "pending")
            .OrderBy(ci => ci.RequestedDate ?? ci.CreatedDate)
            .ThenBy(ci => ci.Id)
            .ToListAsync();

        if (pendingReservations.Count == 0)
        {
            return ReservationPriorityResult.Allow();
        }

        var displayNames = await ResolveOwnerDisplayNamesAsync(pendingReservations.Select(ci => ci.OwnerKey));
        var firstReservation = pendingReservations[0];
        var firstUsername = ResolveReservationUsername(firstReservation.OwnerKey, displayNames);

        if (!string.Equals(firstUsername, username, StringComparison.OrdinalIgnoreCase))
        {
            return ReservationPriorityResult.Deny(
                $"This book is currently reserved. FIFO priority is for {firstUsername}.");
        }

        return ReservationPriorityResult.Allow(firstReservation);
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveOwnerDisplayNamesAsync(IEnumerable<string> ownerKeys)
    {
        var distinctOwnerKeys = ownerKeys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var userIds = distinctOwnerKeys
            .Select(TryExtractUserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        return await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(
                u => u.Id,
                u => string.IsNullOrWhiteSpace(u.FullName) ? (u.UserName ?? "User") : u.FullName);
    }

    private static int CalculateLateDays(DateTime dueDate, DateTime returnedAtUtc)
    {
        if (returnedAtUtc.Date <= dueDate.Date)
        {
            return 0;
        }

        return (returnedAtUtc.Date - dueDate.Date).Days;
    }

    private async Task<string> ResolveBorrowerNameForReservationAsync(string ownerKey)
    {
        var ownerNames = await ResolveOwnerDisplayNamesAsync(new[] { ownerKey });
        return ResolveReservationUsername(ownerKey, ownerNames);
    }

    private static string BuildReservationBorrowingSource(int reservationId)
    {
        return $"reservation:{reservationId}";
    }

    private static string NormalizeUsername(string? username)
    {
        return (username ?? string.Empty).Trim();
    }

    private static string NormalizeBorrowingStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return string.Empty;
        }

        var normalized = status.Trim().ToLowerInvariant();
        return BorrowingStatuses.Contains(normalized) ? normalized : string.Empty;
    }

    private static string NormalizeReservationStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return string.Empty;
        }

        var normalized = status.Trim().ToLowerInvariant();
        return ReservationStatuses.Contains(normalized) ? normalized : string.Empty;
    }

    private static string? TryExtractUserId(string ownerKey)
    {
        if (!ownerKey.StartsWith("user:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var value = ownerKey["user:".Length..].Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string ResolveReservationUsername(string ownerKey, IReadOnlyDictionary<string, string> usersById)
    {
        var userId = TryExtractUserId(ownerKey);
        if (!string.IsNullOrWhiteSpace(userId) && usersById.TryGetValue(userId, out var userDisplay))
        {
            return userDisplay;
        }

        if (ownerKey.StartsWith("guest:", StringComparison.OrdinalIgnoreCase))
        {
            var guestCode = ownerKey["guest:".Length..];
            if (guestCode.Length > 6)
            {
                guestCode = guestCode[..6];
            }
            return $"Guest-{guestCode}";
        }

        return "User";
    }

    public sealed class CreateBorrowingRequest
    {
        public string Username { get; set; } = string.Empty;
        public string BookCode { get; set; } = string.Empty;
        public DateTime? BorrowDate { get; set; }
    }

    public sealed class UpdateBorrowingRequest
    {
        public string Username { get; set; } = string.Empty;
        public string BookCode { get; set; } = string.Empty;
        public DateTime? BorrowDate { get; set; }
    }

    public sealed class UpdateReservationRequest
    {
        public string BookCode { get; set; } = string.Empty;
        public DateTime? ReservationDate { get; set; }
    }

    private sealed class ReservationPriorityResult
    {
        public bool Allowed { get; init; }
        public string Message { get; init; } = string.Empty;
        public Models.CartItem? MatchedReservation { get; init; }

        public static ReservationPriorityResult Allow(Models.CartItem? matchedReservation = null)
        {
            return new ReservationPriorityResult
            {
                Allowed = true,
                MatchedReservation = matchedReservation
            };
        }

        public static ReservationPriorityResult Deny(string message)
        {
            return new ReservationPriorityResult
            {
                Allowed = false,
                Message = message
            };
        }
    }
}
