namespace Library_Management_system.Models.Admin
{
    public sealed class ManageBorrowingViewModel
    {
        public IReadOnlyList<BorrowingRowViewModel> Borrowings { get; set; } = Array.Empty<BorrowingRowViewModel>();
        public IReadOnlyList<ReservationRowViewModel> Reservations { get; set; } = Array.Empty<ReservationRowViewModel>();
        public IReadOnlyList<BookOptionViewModel> BookOptions { get; set; } = Array.Empty<BookOptionViewModel>();
        public string BorrowingQuery { get; set; } = string.Empty;
        public string BorrowingStatus { get; set; } = string.Empty;
        public string ReservationQuery { get; set; } = string.Empty;
        public string ReservationStatus { get; set; } = string.Empty;
    }

    public sealed class BorrowingRowViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public int BookId { get; set; }
        public string BookCode { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public string CreatedBy { get; set; } = "System";
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; } = "active";
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
}
