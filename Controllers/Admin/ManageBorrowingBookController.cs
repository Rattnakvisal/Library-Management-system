using Library_Management_system.Data;
using Library_Management_system.Models;
using Library_Management_system.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Library_Management_system.Controllers.Admin;

[Authorize(Roles = "Admin,Librarian")]
[Route("admin/manageborrowingbook")]
public class ManageBorrowingBookController : Controller
{
    private const int DefaultBorrowingDays = 14;
    private const int MaxActiveBorrowingsPerUser = 3;
    private const decimal FinePerLateDay = 1.00m;
    private const int DefaultPageSize = 10;

    private static readonly HashSet<string> BorrowingStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "active",
        "overdue",
        "returned",
        "rejected"
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
        string? rs = null,
        int bp = 1,
        int rp = 1)
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

        var borrowingIds = borrowingsRaw.Select(x => x.Id).ToList();
        var fineByBorrowingId = borrowingIds.Count == 0
            ? new Dictionary<int, Fine>()
            : await _context.Fines
                .AsNoTracking()
                .Where(f => borrowingIds.Contains(f.BorrowID))
                .ToDictionaryAsync(f => f.BorrowID);

        var borrowingRows = borrowingsRaw
            .Select(x =>
            {
                var status = ComputeBorrowingStatus(x.Status, x.DueDate, x.ReturnDate);
                fineByBorrowingId.TryGetValue(x.Id, out var fine);
                return new BorrowingRowViewModel
                {
                    Id = x.Id,
                    Username = x.Username,
                    ReservationId = x.ReservationId,
                    BookId = x.BookId,
                    BookCode = string.IsNullOrWhiteSpace(x.Book?.BookCode) ? $"A{x.BookId:0000}" : x.Book!.BookCode,
                    BookTitle = x.Book?.Title ?? "(Missing book)",
                    BorrowDate = x.BorrowDate,
                    DueDate = x.DueDate,
                    DurationDays = x.DurationDays <= 0 ? DefaultBorrowingDays : x.DurationDays,
                    ReturnDate = x.ReturnDate,
                    ReturnUserId = x.ReturnUserId,
                    CreatedBy = string.IsNullOrWhiteSpace(x.CreatedBy) ? "System" : x.CreatedBy,
                    CreatedDate = x.CreatedDate == default ? x.BorrowDate : x.CreatedDate,
                    Status = status,
                    FineAmount = fine?.Amount ?? 0m,
                    IsFinePaid = fine?.Paid ?? false,
                    FinePaidDate = fine?.PaidDate,
                    FineRemark = fine?.Remark
                };
            })
            .Where(x =>
                (string.IsNullOrWhiteSpace(borrowingKeyword) ||
                 x.Username.Contains(borrowingKeyword, StringComparison.OrdinalIgnoreCase) ||
                 x.BookTitle.Contains(borrowingKeyword, StringComparison.OrdinalIgnoreCase) ||
                 x.BookCode.Contains(borrowingKeyword, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(borrowingStatus) || x.Status == borrowingStatus))
            .ToList();

        var borrowingTotalPages = Math.Max(1, (int)Math.Ceiling(borrowingRows.Count / (double)DefaultPageSize));
        var borrowingPage = Math.Clamp(bp, 1, borrowingTotalPages);
        var pagedBorrowingRows = borrowingRows
            .Skip((borrowingPage - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .ToList();

        var reservationCandidates = await _context.CartItems
            .AsNoTracking()
            .Include(ci => ci.Book)
            .Where(ci => ci.ReservationStatus != "none")
            .OrderBy(ci => ci.RequestedDate ?? ci.CreatedDate)
            .ThenBy(ci => ci.Id)
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

        var reservationTotalPages = Math.Max(1, (int)Math.Ceiling(reservationRows.Count / (double)DefaultPageSize));
        var reservationPage = Math.Clamp(rp, 1, reservationTotalPages);
        var pagedReservationRows = reservationRows
            .Skip((reservationPage - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
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

        var userOptions = (await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.FullName)
                .ThenBy(u => u.UserName)
                .Select(u => new { u.FullName, u.UserName })
                .ToListAsync())
            .Select(u =>
            {
                var fullName = (u.FullName ?? string.Empty).Trim();
                var userName = (u.UserName ?? string.Empty).Trim();
                var selectedUsername = !string.IsNullOrWhiteSpace(fullName) ? fullName : userName;
                if (string.IsNullOrWhiteSpace(selectedUsername))
                {
                    return null;
                }

                var displayName = !string.IsNullOrWhiteSpace(userName) &&
                                  !string.Equals(fullName, userName, StringComparison.OrdinalIgnoreCase)
                    ? $"{selectedUsername} ({userName})"
                    : selectedUsername;

                return new UserOptionViewModel
                {
                    Username = selectedUsername,
                    DisplayName = displayName
                };
            })
            .Where(u => u != null)
            .Select(u => u!)
            .DistinctBy(u => u.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var model = new ManageBorrowingViewModel
        {
            Borrowings = pagedBorrowingRows,
            Reservations = pagedReservationRows,
            BookOptions = bookOptions,
            UserOptions = userOptions,
            BorrowingQuery = borrowingKeyword,
            BorrowingStatus = borrowingStatus,
            ReservationQuery = reservationKeyword,
            ReservationStatus = reservationStatus,
            BorrowingPage = borrowingPage,
            BorrowingTotalPages = borrowingTotalPages,
            ReservationPage = reservationPage,
            ReservationTotalPages = reservationTotalPages,
            PageSize = DefaultPageSize
        };

        return View("~/Views/Admin/ManageBorrowingBook/Index.cshtml", model);
    }

    [HttpPost("borrowing/create")]
    public async Task<IActionResult> CreateBorrowing([FromForm] CreateBorrowingRequest request)
    {
        var userResolution = await ResolveBorrowingUsernameAsync(request.Username);
        var normalizedUsername = userResolution.Username;
        if (string.IsNullOrWhiteSpace(normalizedUsername) ||
            string.IsNullOrWhiteSpace(request.BookCode))
        {
            return BadRequest(new { success = false, message = "Username and Book Code are required." });
        }

        if (!userResolution.Exists)
        {
            return BadRequest(new { success = false, message = "Selected user was not found." });
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
        var durationDays = DefaultBorrowingDays;
        var dueDate = borrowDate.AddDays(durationDays);
        var status = ComputeOpenBorrowingStatus(dueDate);
        var reservationId = reservationPriority.MatchedReservation?.Id;
        var source = reservationId.HasValue ? BuildReservationBorrowingSource(reservationId.Value) : "in_person";

        _context.BorrowingRecords.Add(new Models.BorrowingRecord
        {
            Username = normalizedUsername,
            ReservationId = reservationId,
            BookId = book.Id,
            BorrowDate = borrowDate,
            DueDate = dueDate,
            DurationDays = durationDays,
            Status = status,
            ReturnDate = null,
            ReturnUserId = null,
            Source = source,
            CreatedBy = User?.Identity?.Name,
            CreatedDate = DateTime.UtcNow
        });

        if (reservationPriority.MatchedReservation is { } matchedReservation)
        {
            matchedReservation.IsRequested = true;
            matchedReservation.ReservationStatus = "approved";
            matchedReservation.RequestedDate ??= DateTime.UtcNow;
            matchedReservation.ReservationUpdatedDate = DateTime.UtcNow;
            matchedReservation.IsReservationNotificationSeen = false;
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

        if (string.Equals(borrowing.Status, "returned", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(borrowing.Status, "rejected", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "Closed borrowing cannot be edited." });
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
        var durationDays = borrowing.DurationDays > 0 ? borrowing.DurationDays : DefaultBorrowingDays;
        var dueDate = borrowDate.AddDays(durationDays);
        borrowing.Username = normalizedUsername;
        borrowing.BorrowDate = borrowDate;
        borrowing.DueDate = dueDate;
        borrowing.DurationDays = durationDays;
        borrowing.ReturnDate = null;
        borrowing.ReturnUserId = null;
        borrowing.Status = ComputeOpenBorrowingStatus(dueDate);

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

        if (string.Equals(borrowing.Status, "rejected", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "This borrowing was rejected and cannot be returned." });
        }

        var returnedAt = DateTime.UtcNow;
        borrowing.Status = "returned";
        borrowing.ReturnDate = returnedAt;
        borrowing.ReturnUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.Identity?.Name;

        if (borrowing.Book != null)
        {
            borrowing.Book.Quantity += 1;
        }

        var lateDays = CalculateLateDays(borrowing.DueDate, returnedAt);
        var fineAmount = lateDays > 0 ? lateDays * FinePerLateDay : 0m;
        Fine? fine = null;
        if (lateDays > 0)
        {
            fine = await _context.Fines.FirstOrDefaultAsync(f => f.BorrowID == borrowing.Id);
            if (fine == null)
            {
                fine = new Fine
                {
                    BorrowID = borrowing.Id,
                    Amount = fineAmount,
                    Paid = false,
                    PaidDate = null,
                    Remark = $"Auto-generated for {lateDays} late day(s)."
                };
                _context.Fines.Add(fine);
            }
            else
            {
                fine.Amount = fineAmount;
                fine.Remark ??= $"Auto-generated for {lateDays} late day(s).";
            }
        }

        var nextPendingReservation = await _context.CartItems
            .Where(ci => ci.BookId == borrowing.BookId && ci.ReservationStatus == "pending")
            .OrderBy(ci => ci.RequestedDate ?? ci.CreatedDate)
            .ThenBy(ci => ci.Id)
            .FirstOrDefaultAsync();

        string message;
        if (lateDays > 0)
        {
            var paymentStatus = fine?.Paid == true ? "paid" : "unpaid";
            message = $"Book marked as returned. Overdue by {lateDays} day(s). Fine: ${fineAmount:0.00} ({paymentStatus}).";
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

    [HttpPost("fine/mark-paid/{borrowingId:int}")]
    public async Task<IActionResult> MarkFineAsPaid(int borrowingId, [FromForm] string? remark)
    {
        var fine = await _context.Fines.FirstOrDefaultAsync(x => x.BorrowID == borrowingId);
        if (fine == null)
        {
            return NotFound(new { success = false, message = "Fine record not found for this borrowing." });
        }

        if (fine.Paid)
        {
            return BadRequest(new { success = false, message = "This fine is already marked as paid." });
        }

        fine.Paid = true;
        fine.PaidDate = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(remark))
        {
            var trimmed = remark.Trim();
            fine.Remark = trimmed.Length > 1000 ? trimmed[..1000] : trimmed;
        }
        else if (string.IsNullOrWhiteSpace(fine.Remark))
        {
            fine.Remark = "Marked as paid by librarian.";
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = $"Fine marked as paid (${fine.Amount:0.00})." });
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
            .AnyAsync(br => br.ReservationId == id || br.Source == sourceKey);

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
                ReservationId = reservation.Id,
                BookId = reservation.BookId,
                BorrowDate = syncBorrowDate,
                DueDate = syncDueDate,
                DurationDays = DefaultBorrowingDays,
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
        var earlierPendingReservation = await _context.CartItems
            .AsNoTracking()
            .Where(ci => ci.BookId == reservation.BookId &&
                         ci.ReservationStatus == "pending" &&
                         ci.Id != reservation.Id)
            .Where(ci =>
                (ci.RequestedDate ?? ci.CreatedDate) < reservationOrderDate ||
                ((ci.RequestedDate ?? ci.CreatedDate) == reservationOrderDate && ci.Id < reservation.Id))
            .OrderBy(ci => ci.RequestedDate ?? ci.CreatedDate)
            .ThenBy(ci => ci.Id)
            .FirstOrDefaultAsync();

        if (earlierPendingReservation != null)
        {
            var priorityUser = await ResolveBorrowerNameForReservationAsync(earlierPendingReservation.OwnerKey);
            return BadRequest(new
            {
                success = false,
                message = $"This reservation cannot be approved yet. FIFO priority is for {priorityUser} (reservation #{earlierPendingReservation.Id})."
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
                ReservationId = reservation.Id,
                BookId = reservation.BookId,
                BorrowDate = borrowDate,
                DueDate = dueDate,
                DurationDays = DefaultBorrowingDays,
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
        reservation.IsReservationNotificationSeen = false;

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Reservation approved and stored in borrowing with a 2-week due date." });
    }

    [HttpPost("reservation/reject/{id:int}")]
    public async Task<IActionResult> RejectReservation(int id, [FromForm] string? reason)
    {
        var reservation = await _context.CartItems
            .Include(ci => ci.Book)
            .FirstOrDefaultAsync(ci => ci.Id == id);
        if (reservation == null)
        {
            return NotFound(new { success = false, message = "Reservation not found." });
        }

        var rejectionReason = string.IsNullOrWhiteSpace(reason)
            ? "Reservation rejected by librarian."
            : reason.Trim();
        if (rejectionReason.Length > 100)
        {
            rejectionReason = rejectionReason[..100];
        }
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.Identity?.Name;
        var currentStatus = NormalizeReservationStatus(reservation.ReservationStatus);
        if (currentStatus == "approved" && reservation.Book != null)
        {
            reservation.Book.Quantity += 1;

            var linkedBorrowing = await _context.BorrowingRecords
                .Where(br => br.ReservationId == reservation.Id || br.Source == BuildReservationBorrowingSource(reservation.Id))
                .OrderByDescending(br => br.BorrowDate)
                .ThenByDescending(br => br.Id)
                .FirstOrDefaultAsync();

            if (linkedBorrowing != null &&
                !string.Equals(linkedBorrowing.Status, "returned", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(linkedBorrowing.Status, "rejected", StringComparison.OrdinalIgnoreCase))
            {
                linkedBorrowing.Status = "rejected";
                linkedBorrowing.ReturnDate = DateTime.UtcNow;
                linkedBorrowing.ReturnUserId = currentUserId;
            }
        }

        reservation.IsRequested = false;
        reservation.ReservationStatus = "rejected";
        reservation.ReservationUpdatedDate = DateTime.UtcNow;
        reservation.RequestedDate ??= DateTime.UtcNow;
        reservation.IsReservationNotificationSeen = false;

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
        if (string.Equals(currentStatus, "rejected", StringComparison.OrdinalIgnoreCase))
        {
            return "rejected";
        }

        if (string.Equals(currentStatus, "returned", StringComparison.OrdinalIgnoreCase))
        {
            return "returned";
        }

        if (returnDate.HasValue &&
            !string.Equals(currentStatus, "active", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(currentStatus, "overdue", StringComparison.OrdinalIgnoreCase))
        {
            return "returned";
        }

        return ComputeOpenBorrowingStatus(dueDate);
    }

    private static string ComputeOpenBorrowingStatus(DateTime dueDate)
    {
        return dueDate.Date < DateTime.UtcNow.Date ? "overdue" : "active";
    }

    private async Task<(int ActiveCount, int OverdueCount)> GetBorrowingStateAsync(string username, int? excludeBorrowingId = null)
    {
        var query = _context.BorrowingRecords
            .AsNoTracking()
            .Where(br => br.Username == username &&
                         br.Status != "returned" &&
                         br.Status != "rejected");

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

    private async Task<(string Username, bool Exists)> ResolveBorrowingUsernameAsync(string? username)
    {
        var normalized = NormalizeUsername(username);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return (string.Empty, false);
        }

        var matchedUser = await _context.Users
            .AsNoTracking()
            .Where(u => u.FullName == normalized || u.UserName == normalized)
            .Select(u => new { u.FullName, u.UserName })
            .FirstOrDefaultAsync();

        if (matchedUser == null)
        {
            return (normalized, false);
        }

        var resolvedName = NormalizeUsername(matchedUser.FullName);
        if (string.IsNullOrWhiteSpace(resolvedName))
        {
            resolvedName = NormalizeUsername(matchedUser.UserName);
        }

        return (resolvedName, !string.IsNullOrWhiteSpace(resolvedName));
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
