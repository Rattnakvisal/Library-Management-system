namespace Library_Management_system.Models.Admin
{
    public sealed class ManageBorrowingViewModel
    {
        public IReadOnlyList<BorrowingRowViewModel> Borrowings { get; set; } = Array.Empty<BorrowingRowViewModel>();
        public IReadOnlyList<ReservationRowViewModel> Reservations { get; set; } = Array.Empty<ReservationRowViewModel>();
        public IReadOnlyList<BookOptionViewModel> BookOptions { get; set; } = Array.Empty<BookOptionViewModel>();
        public IReadOnlyList<UserOptionViewModel> UserOptions { get; set; } = Array.Empty<UserOptionViewModel>();
        public string BorrowingQuery { get; set; } = string.Empty;
        public string BorrowingStatus { get; set; } = string.Empty;
        public string ReservationQuery { get; set; } = string.Empty;
        public string ReservationStatus { get; set; } = string.Empty;
        public int BorrowingPage { get; set; } = 1;
        public int BorrowingTotalPages { get; set; } = 1;
        public int ReservationPage { get; set; } = 1;
        public int ReservationTotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public sealed class BorrowingRowViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public int? ReservationId { get; set; }
        public int BookId { get; set; }
        public string BookCode { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public int DurationDays { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string? ReturnUserId { get; set; }
        public string CreatedBy { get; set; } = "System";
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; } = "active";
        public decimal FineAmount { get; set; }
        public bool IsFinePaid { get; set; }
        public DateTime? FinePaidDate { get; set; }
        public string? FineRemark { get; set; }
    }

    public sealed class ReservationRowViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public int BookId { get; set; }
        public string BookCode { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public DateTime ReservationDate { get; set; }
        public string Status { get; set; } = "pending";
    }

    public sealed class BookOptionViewModel
    {
        public int BookId { get; set; }
        public string BookCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public sealed class UserOptionViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}
