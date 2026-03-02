namespace Library_Management_system.Models
{
    public sealed class HistoryViewModel
    {
        public IReadOnlyList<HistoryItemViewModel> Items { get; set; } = Array.Empty<HistoryItemViewModel>();
        public int TotalCount { get; set; }
        public int OnHoldCount { get; set; }
        public int ReturnedCount { get; set; }
        public decimal EstimatedFine { get; set; }
    }

    public sealed class HistoryItemViewModel
    {
        public int BorrowingId { get; set; }
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public int Year { get; set; }
        public string BookCode { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Status { get; set; } = "borrowing";
        public string StatusLabel { get; set; } = "Borrowing";
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public int LateDays { get; set; }
        public decimal FineAmount { get; set; }
    }
}
