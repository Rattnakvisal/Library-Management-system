namespace Library_Management_system.Models
{
    public sealed class BookmarkViewModel
    {
        public IReadOnlyList<BookmarkItemViewModel> Items { get; set; } = Array.Empty<BookmarkItemViewModel>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
    }

    public sealed class BookmarkItemViewModel
    {
        public int BookId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string BookCode { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; }
    }
}
