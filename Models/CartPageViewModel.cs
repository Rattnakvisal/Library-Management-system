namespace Library_Management_system.Models
{
    public sealed class CartPageViewModel
    {
        public IReadOnlyList<CartItemCardViewModel> Items { get; set; } = Array.Empty<CartItemCardViewModel>();
        public int TotalBooks { get; set; }
        public int RequestedBooks { get; set; }
        public int ApprovedReservationsCount { get; set; }
        public int RejectedReservationsCount { get; set; }
        public long LastReservationDecisionVersion { get; set; }
    }

    public sealed class CartItemCardViewModel
    {
        public int CartItemId { get; set; }
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string BookCode { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Rating { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsRequested { get; set; }
        public string ReservationStatus { get; set; } = "none";
    }
}
