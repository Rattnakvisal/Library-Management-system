using Library_Management_system.Data;
using Library_Management_system.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace Library_Management_system.Controllers
{
    public class HomeController : Controller
    {
        private const string ProfileImageClaimType = "ProfileImageUrl";
        private const string CoverImageClaimType = "CoverImageUrl";
        private const string DefaultProfileImageUrl =
            "https://i.pinimg.com/736x/c0/d1/9f/c0d19f67852cb44fe9bdbed793141790.jpg";
        private const string DefaultCoverImageUrl =
            "https://i.pinimg.com/736x/61/0d/c5/610dc55e2fb7a1ab8728a718be63e1d4.jpg";

        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _environment = environment;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index()
        {
            const int trendingBooksLimit = 4;
            const int newArrivalsLimit = 4;

            // Get categories with images
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Take(3)
                .ToListAsync();

            // Get trending/popular books (highest rated or most recent)
            var trendingBooks = await _context.Books
                .AsNoTracking()
                .OrderByDescending(b => b.Rating)
                .ThenByDescending(b => b.Id)
                .Take(trendingBooksLimit)
                .ToListAsync();

            // Get new arrival books
            var newArrivalBooks = await _context.Books
                .AsNoTracking()
                .OrderByDescending(b => b.Id)
                .Take(newArrivalsLimit)
                .ToListAsync();

            // Get genres/categories for the genre section
            var genres = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Take(4)
                .ToListAsync();

            var model = new HomeViewModel
            {
                Categories = categories,
                TrendingBooks = trendingBooks,
                NewArrivalBooks = newArrivalBooks,
                CategoryGenres = genres
            };

            return View(model);
        }

        [HttpGet("about")]
        public IActionResult About()
        {
            ViewBag.Title = "About";
            return View("~/Views/User/About/about.cshtml");
        }

        [HttpGet("about/faq")]
        public IActionResult AboutFaq()
        {
            ViewBag.Title = "About - FAQ";
            return View("~/Views/User/About/faq.cshtml");
        }

        [HttpGet("about/support")]
        public IActionResult AboutSupport()
        {
            ViewBag.Title = "About - Support";
            return View("~/Views/User/About/support.cshtml");
        }

        [HttpGet("about/policies")]
        public IActionResult AboutPolicies()
        {
            ViewBag.Title = "About - Policies";
            return View("~/Views/User/About/policies.cshtml");
        }

        [HttpGet("about/account")]
        public IActionResult AboutAccount()
        {
            ViewBag.Title = "About - Account";
            return View("~/Views/User/About/account.cshtml");
        }

        [HttpGet("/login")]
        public IActionResult Login()
        {
            ViewBag.Title = "About - Login";
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        [HttpGet("event")]
        public async Task<IActionResult> Event()
        {
            ViewBag.Title = "Events";

            var events = await _context.Events
                .AsNoTracking()
                .OrderBy(e => e.StartDate)
                .ThenBy(e => e.Id)
                .ToListAsync();

            return View("~/Views/User/Event/Event.cshtml", events);
        }

        [HttpGet("contact")]
        public IActionResult Contact()
        {
            ViewBag.Title = "Contact";
            return View("~/Views/User/Contact/Contact.cshtml");
        }

        [HttpGet("history")]
        public async Task<IActionResult> History()
        {
            ViewBag.Title = "History";

            var ownerKey = ResolveCartOwnerKey();
            var usernameCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var currentUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (currentUser != null)
                {
                    if (!string.IsNullOrWhiteSpace(currentUser.FullName))
                    {
                        usernameCandidates.Add(currentUser.FullName.Trim());
                    }

                    if (!string.IsNullOrWhiteSpace(currentUser.UserName))
                    {
                        usernameCandidates.Add(currentUser.UserName.Trim());
                    }

                    if (!string.IsNullOrWhiteSpace(currentUser.Email))
                    {
                        usernameCandidates.Add(currentUser.Email.Trim());
                    }
                }
            }

            var reservationSourceKeys = await _context.CartItems
                .AsNoTracking()
                .Where(ci => ci.OwnerKey == ownerKey)
                .Select(ci => $"reservation:{ci.Id}")
                .ToListAsync();

            var borrowingRecords = new List<BorrowingRecord>();

            if (usernameCandidates.Count > 0)
            {
                var byUsername = await _context.BorrowingRecords
                    .AsNoTracking()
                    .Include(br => br.Book)
                    .Where(br => usernameCandidates.Contains(br.Username))
                    .OrderByDescending(br => br.BorrowDate)
                    .ThenByDescending(br => br.Id)
                    .ToListAsync();

                borrowingRecords.AddRange(byUsername);
            }

            if (reservationSourceKeys.Count > 0)
            {
                var byReservationSource = await _context.BorrowingRecords
                    .AsNoTracking()
                    .Include(br => br.Book)
                    .Where(br => reservationSourceKeys.Contains(br.Source))
                    .OrderByDescending(br => br.BorrowDate)
                    .ThenByDescending(br => br.Id)
                    .ToListAsync();

                borrowingRecords.AddRange(byReservationSource);
            }

            var normalizedRecords = borrowingRecords
                .GroupBy(br => br.Id)
                .Select(g => g.First())
                .OrderByDescending(br => br.BorrowDate)
                .ThenByDescending(br => br.Id)
                .ToList();

            const decimal finePerLateDay = 1.00m;
            var nowUtc = DateTime.UtcNow;

            var items = normalizedRecords
                .Select(br =>
                {
                    var status = ComputeHistoryStatus(br, nowUtc);
                    var statusLabel = status switch
                    {
                        "returned" => "Returned",
                        "overdue" => "Overdue",
                        _ => "Borrowing"
                    };
                    var fineDate = br.ReturnDate ?? nowUtc;
                    var lateDays = status == "borrowing" ? 0 : CalculateLateDaysForHistory(br.DueDate, fineDate);
                    var fineAmount = lateDays * finePerLateDay;

                    return new HistoryItemViewModel
                    {
                        BorrowingId = br.Id,
                        BookId = br.BookId,
                        Title = br.Book?.Title ?? "(Missing book)",
                        Author = br.Book?.Author ?? "Unknown",
                        Year = br.Book?.Year ?? 0,
                        BookCode = string.IsNullOrWhiteSpace(br.Book?.BookCode) ? $"A{br.BookId:0000}" : br.Book!.BookCode,
                        Rating = Math.Clamp(br.Book?.Rating ?? 0, 0, 5),
                        ImageUrl = string.IsNullOrWhiteSpace(br.Book?.ImageUrl) ? "/images/User/Book/book2.png" : br.Book!.ImageUrl,
                        Status = status,
                        StatusLabel = statusLabel,
                        BorrowDate = br.BorrowDate,
                        DueDate = br.DueDate,
                        ReturnDate = br.ReturnDate,
                        LateDays = lateDays,
                        FineAmount = fineAmount
                    };
                })
                .ToList();

            var onHoldCount = items.Count(x => x.Status != "returned");
            var returnedCount = items.Count(x => x.Status == "returned");
            var estimatedFine = items.Sum(x => x.FineAmount);

            var model = new HistoryViewModel
            {
                Items = items,
                TotalCount = items.Count,
                OnHoldCount = onHoldCount,
                ReturnedCount = returnedCount,
                EstimatedFine = estimatedFine
            };

            return View("~/Views/User/History/History.cshtml", model);
        }

        [HttpGet("cart")]
        public async Task<IActionResult> Cart()
        {
            ViewBag.Title = "Cart";

            var ownerKey = ResolveCartOwnerKey();
            var items = await _context.CartItems
                .AsNoTracking()
                .Include(ci => ci.Book)
                .Where(ci => ci.OwnerKey == ownerKey && ci.Book != null)
                .OrderByDescending(ci => ci.CreatedDate)
                .ToListAsync();

            var model = new CartPageViewModel
            {
                TotalBooks = items.Count,
                RequestedBooks = items.Count(ci =>
                    string.Equals(ci.ReservationStatus, "pending", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(ci.ReservationStatus, "approved", StringComparison.OrdinalIgnoreCase)),
                ApprovedReservationsCount = items.Count(ci =>
                    string.Equals(ci.ReservationStatus, "approved", StringComparison.OrdinalIgnoreCase)),
                RejectedReservationsCount = items.Count(ci =>
                    string.Equals(ci.ReservationStatus, "rejected", StringComparison.OrdinalIgnoreCase)),
                LastReservationDecisionVersion = items
                    .Where(ci =>
                        string.Equals(ci.ReservationStatus, "approved", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(ci.ReservationStatus, "rejected", StringComparison.OrdinalIgnoreCase))
                    .Select(ci => ci.ReservationUpdatedDate ?? ci.RequestedDate ?? ci.CreatedDate)
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Max()
                    .Ticks,
                Items = items.Select(ci =>
                {
                    var book = ci.Book!;
                    return new CartItemCardViewModel
                    {
                        CartItemId = ci.Id,
                        BookId = book.Id,
                        Title = book.Title,
                        Author = book.Author,
                        BookCode = string.IsNullOrWhiteSpace(book.BookCode) ? $"A{book.Id:0000}" : book.BookCode,
                        Year = book.Year,
                        Rating = Math.Clamp(book.Rating, 0, 5),
                        ImageUrl = string.IsNullOrWhiteSpace(book.ImageUrl) ? "/images/User/Book/book2.png" : book.ImageUrl,
                        CreatedDate = ci.CreatedDate,
                        IsRequested = ci.IsRequested,
                        ReservationStatus = string.IsNullOrWhiteSpace(ci.ReservationStatus) ? "none" : ci.ReservationStatus
                    };
                }).ToList()
            };

            return View("~/Views/User/Cart/Cart.cshtml", model);
        }

        [HttpPost("cart/add/{bookId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int bookId)
        {
            var book = await _context.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null)
            {
                TempData["CartError"] = "Book not found.";
                return RedirectToAction(nameof(Cart));
            }

            var ownerKey = ResolveCartOwnerKey();
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.OwnerKey == ownerKey && ci.BookId == bookId);

            if (existingItem != null)
            {
                TempData["CartMessage"] = "This book is already in your cart.";
                return RedirectToAction(nameof(Cart));
            }

            _context.CartItems.Add(new CartItem
            {
                OwnerKey = ownerKey,
                BookId = bookId,
                CreatedDate = DateTime.UtcNow,
                IsRequested = false,
                ReservationStatus = "none",
                IsReservationNotificationSeen = false
            });

            await _context.SaveChangesAsync();
            TempData["CartMessage"] = "Book added to cart.";
            return RedirectToAction(nameof(Cart));
        }

        [HttpPost("cart/delete-selected")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelectedCartItems([FromForm] int[] itemIds)
        {
            if (itemIds == null || itemIds.Length == 0)
            {
                TempData["CartError"] = "Please select book(s) to delete.";
                return RedirectToAction(nameof(Cart));
            }

            var ownerKey = ResolveCartOwnerKey();
            var itemsToDelete = await _context.CartItems
                .Where(ci => ci.OwnerKey == ownerKey && itemIds.Contains(ci.Id))
                .ToListAsync();

            if (itemsToDelete.Count == 0)
            {
                TempData["CartError"] = "No matching cart items found.";
                return RedirectToAction(nameof(Cart));
            }

            _context.CartItems.RemoveRange(itemsToDelete);
            await _context.SaveChangesAsync();

            TempData["CartMessage"] = $"{itemsToDelete.Count} item(s) removed.";
            return RedirectToAction(nameof(Cart));
        }

        [HttpPost("cart/proceed-request")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProceedToRequest([FromForm] int[] itemIds)
        {
            if (itemIds == null || itemIds.Length == 0)
            {
                TempData["CartError"] = "Please select book(s) to request.";
                return RedirectToAction(nameof(Cart));
            }

            var ownerKey = ResolveCartOwnerKey();
            var itemsToRequest = await _context.CartItems
                .Where(ci => ci.OwnerKey == ownerKey &&
                             itemIds.Contains(ci.Id) &&
                             ci.ReservationStatus != "pending" &&
                             ci.ReservationStatus != "approved")
                .ToListAsync();

            if (itemsToRequest.Count == 0)
            {
                TempData["CartError"] = "Selected item(s) were already requested or not found.";
                return RedirectToAction(nameof(Cart));
            }

            var now = DateTime.UtcNow;
            foreach (var item in itemsToRequest)
            {
                item.IsRequested = true;
                item.RequestedDate = now;
                item.ReservationStatus = "pending";
                item.ReservationUpdatedDate = now;
                item.IsReservationNotificationSeen = false;
            }

            await _context.SaveChangesAsync();

            TempData["CartMessage"] = $"Request submitted for {itemsToRequest.Count} item(s).";
            return RedirectToAction(nameof(Cart));
        }

        [HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
            ViewBag.Title = "Profile";

            var fullName = "User";
            var email = string.Empty;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var currentUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == userId);

                if (currentUser != null)
                {
                    fullName = string.IsNullOrWhiteSpace(currentUser.FullName)
                        ? currentUser.UserName ?? "User"
                        : currentUser.FullName;
                    email = currentUser.Email ?? string.Empty;
                }
            }

            var profileImageUrl = DefaultProfileImageUrl;
            var coverImageUrl = DefaultCoverImageUrl;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var imageClaims = await _context.UserClaims
                    .AsNoTracking()
                    .Where(x => x.UserId == userId &&
                                (x.ClaimType == ProfileImageClaimType || x.ClaimType == CoverImageClaimType))
                    .ToListAsync();

                var profileClaim = imageClaims
                    .LastOrDefault(x => x.ClaimType == ProfileImageClaimType)?.ClaimValue;
                var coverClaim = imageClaims
                    .LastOrDefault(x => x.ClaimType == CoverImageClaimType)?.ClaimValue;

                if (!string.IsNullOrWhiteSpace(profileClaim))
                {
                    profileImageUrl = profileClaim;
                }

                if (!string.IsNullOrWhiteSpace(coverClaim))
                {
                    coverImageUrl = coverClaim;
                }
            }

            var ownerKey = ResolveCartOwnerKey();
            var favoriteBooks = await _context.FavoriteBooks
                .AsNoTracking()
                .Include(x => x.Book)
                .Where(x => x.OwnerKey == ownerKey && x.Book != null)
                .OrderByDescending(x => x.CreatedDate)
                .Take(8)
                .ToListAsync();

            var model = new ProfileViewModel
            {
                FullName = fullName,
                Email = email,
                ProfileImageUrl = profileImageUrl,
                CoverImageUrl = coverImageUrl,
                Interests = favoriteBooks.Select(x =>
                {
                    var book = x.Book!;
                    var rating = book.Rating < 0 ? 0 : (book.Rating > 5 ? 5 : book.Rating);

                    return new ProfileInterestItemViewModel
                    {
                        BookId = book.Id,
                        CategoryName = string.IsNullOrWhiteSpace(book.CategoryName)
                            ? "Professional and Technical"
                            : book.CategoryName,
                        Title = book.Title,
                        Author = book.Author,
                        ImageUrl = string.IsNullOrWhiteSpace(book.ImageUrl)
                            ? "/images/User/Book/book2.png"
                            : book.ImageUrl,
                        Rating = rating
                    };
                }).ToList()
            };

            return View("~/Views/User/Profile/Profile.cshtml", model);
        }

        [HttpPost("profile/upload-profile-image")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfileImage(IFormFile? imageFile)
        {
            return await SaveProfileImageAsync(imageFile, ProfileImageClaimType, "Profile image updated successfully.");
        }

        [HttpPost("profile/upload-cover-image")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCoverImage(IFormFile? imageFile)
        {
            return await SaveProfileImageAsync(imageFile, CoverImageClaimType, "Cover image updated successfully.");
        }

        [HttpGet("bookmark")]
        public async Task<IActionResult> Bookmark(int page = 1)
        {
            ViewBag.Title = "Bookmark";
            const int pageSize = 8;

            var ownerKey = ResolveCartOwnerKey();
            var favoritesQuery = _context.FavoriteBooks
                .AsNoTracking()
                .Include(x => x.Book)
                .Where(x => x.OwnerKey == ownerKey && x.Book != null)
                .OrderByDescending(x => x.CreatedDate);

            var totalItems = await favoritesQuery.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            var currentPage = Math.Clamp(page, 1, totalPages);

            var favorites = await favoritesQuery
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new BookmarkViewModel
            {
                CurrentPage = currentPage,
                TotalPages = totalPages,
                TotalItems = totalItems,
                Items = favorites.Select(x =>
                {
                    var book = x.Book!;
                    return new BookmarkItemViewModel
                    {
                        BookId = x.BookId,
                        CategoryName = string.IsNullOrWhiteSpace(book.CategoryName) ? "Professional and Technical" : book.CategoryName,
                        Title = book.Title,
                        Author = book.Author,
                        BookCode = string.IsNullOrWhiteSpace(book.BookCode) ? $"A{book.Id:0000}" : book.BookCode,
                        Rating = Math.Clamp(book.Rating, 0, 5),
                        ImageUrl = string.IsNullOrWhiteSpace(book.ImageUrl) ? "/images/User/Book/book2.png" : book.ImageUrl,
                        AddedAt = x.CreatedDate
                    };
                }).ToList()
            };

            return View("~/Views/User/Bookmark/Bookmark.cshtml", model);
        }

        [HttpPost("bookmark/add/{bookId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFavorite(int bookId, [FromForm] string? returnUrl = null)
        {
            var bookExists = await _context.Books
                .AsNoTracking()
                .AnyAsync(b => b.Id == bookId);

            if (!bookExists)
            {
                return NotFound();
            }

            var ownerKey = ResolveCartOwnerKey();
            var existing = await _context.FavoriteBooks
                .FirstOrDefaultAsync(x => x.OwnerKey == ownerKey && x.BookId == bookId);

            if (existing == null)
            {
                _context.FavoriteBooks.Add(new FavoriteBook
                {
                    OwnerKey = ownerKey,
                    BookId = bookId,
                    CreatedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToLocalOrBookDetail(returnUrl, bookId);
        }

        [HttpPost("bookmark/remove/{bookId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFavorite(int bookId, [FromForm] string? returnUrl = null)
        {
            var ownerKey = ResolveCartOwnerKey();
            var existing = await _context.FavoriteBooks
                .FirstOrDefaultAsync(x => x.OwnerKey == ownerKey && x.BookId == bookId);

            if (existing != null)
            {
                _context.FavoriteBooks.Remove(existing);
                await _context.SaveChangesAsync();
            }

            return RedirectToLocalOrBookDetail(returnUrl, bookId);
        }

        [HttpPost("notifications/reservations/mark-read")]
        public async Task<IActionResult> MarkReservationNotificationsRead()
        {
            var ownerKey = ResolveCartOwnerKey();
            var unreadItems = await _context.CartItems
                .Where(ci => ci.OwnerKey == ownerKey &&
                             (ci.ReservationStatus == "approved" || ci.ReservationStatus == "rejected") &&
                             !ci.IsReservationNotificationSeen)
                .ToListAsync();

            if (unreadItems.Count == 0)
            {
                return Ok(new { success = true, updated = 0 });
            }

            foreach (var item in unreadItems)
            {
                item.IsReservationNotificationSeen = true;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, updated = unreadItems.Count });
        }

        [HttpGet("cookie")]
        public IActionResult Cookie()
        {
            ViewBag.Title = "Cookie";
            return View("~/Views/User/Cookie/Cookie.cshtml");
        }

        //public IActionResult Category()
        //{
        //    return View("~/Views/User/Category/Category.cshtml");
        //}
        //[Area("User")]
        //public IActionResult Category(int page = 1)
        //{
        //    ViewBag.CurrentPage = page;
        //    return View("~/Views/User/Category/Category.cshtml");
        //}

        [HttpGet("book")]
        [HttpGet("[controller]/[action]")]
        public Task<IActionResult> BookIndex(string? category, int page = 1)
        {
            return Category(category, page);
        }

        public async Task<IActionResult> Category(string? category, int page = 1)
        {
            var normalizedCategory = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
            var pageSize = 8;

            var categories = await _context.Books
                .AsNoTracking()
                .Where(b => !string.IsNullOrWhiteSpace(b.CategoryName))
                .Select(b => b.CategoryName)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync();

            var booksQuery = _context.Books.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedCategory))
            {
                booksQuery = booksQuery.Where(b => b.CategoryName == normalizedCategory);
            }

            var totalItems = await booksQuery.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            var currentPage = Math.Clamp(page, 1, totalPages);

            var model = await booksQuery
                .OrderByDescending(b => b.Id)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentCategory = normalizedCategory;
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.Categories = categories;

            return View("~/Views/User/Category/category.cshtml", model);
        }

        [HttpGet("book/{id:int}")]
        public async Task<IActionResult> BookDetail(int id)
        {
            const int recommendationCount = 4;

            var book = await _context.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book is null)
            {
                return NotFound();
            }

            var ownerKey = ResolveCartOwnerKey();
            var isFavorite = await _context.FavoriteBooks
                .AsNoTracking()
                .AnyAsync(x => x.OwnerKey == ownerKey && x.BookId == id);

            var relatedBooks = await _context.Books
                .AsNoTracking()
                .Where(b => b.Id != id && b.CategoryName == book.CategoryName)
                .OrderByDescending(b => b.Id)
                .Take(recommendationCount)
                .ToListAsync();

            if (relatedBooks.Count < recommendationCount)
            {
                var selectedIds = relatedBooks.Select(b => b.Id).ToList();
                var sameCategoryExtras = await _context.Books
                    .AsNoTracking()
                    .Where(b => b.CategoryName == book.CategoryName && !selectedIds.Contains(b.Id))
                    .OrderByDescending(b => b.Id)
                    .Take(recommendationCount - relatedBooks.Count)
                    .ToListAsync();

                relatedBooks.AddRange(sameCategoryExtras);
            }

            if (relatedBooks.Count < recommendationCount)
            {
                var selectedIds = relatedBooks.Select(b => b.Id).ToList();
                var crossCategoryBooks = await _context.Books
                    .AsNoTracking()
                    .Where(b => !selectedIds.Contains(b.Id))
                    .OrderByDescending(b => b.Id)
                    .Take(recommendationCount - relatedBooks.Count)
                    .ToListAsync();

                relatedBooks.AddRange(crossCategoryBooks);
            }

            var model = new BookDetailViewModel
            {
                Book = book,
                RelatedBooks = relatedBooks,
                IsFavorite = isFavorite
            };

            return View("~/Views/User/Books/BookDetail.cshtml", model);
        }

        [HttpPost("book/review")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitBookReview(int bookId, int rating, string? returnUrl = null)
        {
            if (rating < 1 || rating > 5)
            {
                TempData["BookReviewError"] = "Please select a rating between 1 and 5 stars.";
                return RedirectToLocalOrBookDetail(returnUrl, bookId);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                var safeReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
                    ? Url.Action(nameof(BookDetail), new { id = bookId }) ?? "/"
                    : returnUrl;

                return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl = safeReturnUrl });
            }

            var book = await _context.Books.FirstOrDefaultAsync(x => x.Id == bookId);
            if (book == null)
            {
                TempData["BookReviewError"] = "Book not found.";
                return RedirectToLocalOrBookDetail(returnUrl, bookId);
            }

            var existingReview = await _context.BookReviews
                .FirstOrDefaultAsync(x => x.BookId == bookId && x.UserId == user.Id);

            if (existingReview == null)
            {
                _context.BookReviews.Add(new BookReview
                {
                    BookId = bookId,
                    UserId = user.Id,
                    Username = string.IsNullOrWhiteSpace(user.FullName) ? (user.UserName ?? "User") : user.FullName,
                    Email = user.Email ?? string.Empty,
                    Rating = rating,
                    CreatedDate = DateTime.UtcNow
                });
            }
            else
            {
                existingReview.Rating = rating;
                existingReview.Username = string.IsNullOrWhiteSpace(user.FullName) ? (user.UserName ?? "User") : user.FullName;
                existingReview.Email = user.Email ?? string.Empty;
                existingReview.CreatedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            var averageRating = await _context.BookReviews
                .AsNoTracking()
                .Where(x => x.BookId == bookId)
                .AverageAsync(x => (double)x.Rating);

            book.Rating = (int)Math.Round(averageRating, MidpointRounding.AwayFromZero);
            await _context.SaveChangesAsync();

            TempData["BookReviewSuccess"] = "Thank you! Your feedback was submitted.";
            return RedirectToLocalOrBookDetail(returnUrl, bookId);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static string ComputeHistoryStatus(BorrowingRecord borrowing, DateTime nowUtc)
        {
            if (borrowing.ReturnDate.HasValue ||
                string.Equals(borrowing.Status, "returned", StringComparison.OrdinalIgnoreCase))
            {
                return "returned";
            }

            return borrowing.DueDate.Date < nowUtc.Date ? "overdue" : "borrowing";
        }

        private static int CalculateLateDaysForHistory(DateTime dueDate, DateTime compareDate)
        {
            if (compareDate.Date <= dueDate.Date)
            {
                return 0;
            }

            return (compareDate.Date - dueDate.Date).Days;
        }

        private async Task<IActionResult> SaveProfileImageAsync(
            IFormFile? imageFile,
            string claimType,
            string successMessage)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var validationError = ValidateImageFile(imageFile);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                TempData["ProfileError"] = validationError;
                return RedirectToAction(nameof(Profile));
            }

            var imageUrl = await SaveProfileImageFileAsync(imageFile!);
            var existingClaimValue = await GetUserImageClaimValueAsync(user.Id, claimType);

            if (!string.IsNullOrWhiteSpace(existingClaimValue))
            {
                DeleteOwnedProfileImage(existingClaimValue);
            }

            await UpsertUserImageClaimAsync(user, claimType, imageUrl);

            TempData["ProfileSuccess"] = successMessage;
            return RedirectToAction(nameof(Profile));
        }

        private static string? ValidateImageFile(IFormFile? imageFile)
        {
            if (imageFile is null || imageFile.Length == 0)
            {
                return "Please choose an image file.";
            }

            const long maxSizeBytes = 5 * 1024 * 1024;
            if (imageFile.Length > maxSizeBytes)
            {
                return "Image size must be 5 MB or smaller.";
            }

            var extension = Path.GetExtension(imageFile.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            {
                return "Only JPG, JPEG, PNG, and WEBP files are allowed.";
            }

            if (string.IsNullOrWhiteSpace(imageFile.ContentType) ||
                !imageFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return "Invalid image file.";
            }

            return null;
        }

        private async Task<string> SaveProfileImageFileAsync(IFormFile imageFile)
        {
            var uploadsDirectory = Path.Combine(_environment.WebRootPath, "images", "User", "Profile", "uploads");
            Directory.CreateDirectory(uploadsDirectory);

            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsDirectory, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await imageFile.CopyToAsync(stream);

            return $"/images/User/Profile/uploads/{fileName}";
        }

        private async Task<string?> GetUserImageClaimValueAsync(string userId, string claimType)
        {
            return await _context.UserClaims
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.ClaimType == claimType)
                .Select(x => x.ClaimValue)
                .FirstOrDefaultAsync();
        }

        private async Task UpsertUserImageClaimAsync(ApplicationUser user, string claimType, string claimValue)
        {
            var existingClaims = await _userManager.GetClaimsAsync(user);
            var existingClaim = existingClaims.FirstOrDefault(c => c.Type == claimType);
            var newClaim = new Claim(claimType, claimValue);

            if (existingClaim == null)
            {
                await _userManager.AddClaimAsync(user, newClaim);
                return;
            }

            if (existingClaim.Value == claimValue)
            {
                return;
            }

            await _userManager.ReplaceClaimAsync(user, existingClaim, newClaim);
        }

        private void DeleteOwnedProfileImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) ||
                !imageUrl.StartsWith("/images/User/Profile/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var absolutePath = Path.Combine(_environment.WebRootPath, relativePath);

            if (System.IO.File.Exists(absolutePath))
            {
                System.IO.File.Delete(absolutePath);
            }
        }

        private IActionResult RedirectToLocalOrBookDetail(string? returnUrl, int bookId)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(BookDetail), new { id = bookId });
        }

        private string ResolveCartOwnerKey()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                return $"user:{userId}";
            }

            const string cookieName = "library_cart_id";
            if (!Request.Cookies.TryGetValue(cookieName, out var guestCartId) || string.IsNullOrWhiteSpace(guestCartId))
            {
                guestCartId = Guid.NewGuid().ToString("N");
                Response.Cookies.Append(cookieName, guestCartId, new CookieOptions
                {
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = Request.IsHttps,
                    Expires = DateTimeOffset.UtcNow.AddYears(1)
                });
            }

            return $"guest:{guestCartId}";
        }
    }
    
}
