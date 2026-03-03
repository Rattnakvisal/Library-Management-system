using System.ComponentModel.DataAnnotations;

namespace Library_Management_system.Models
{
    public class Book
    {
        [Key]
        public int Id { get; set; }

        public string BookCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? Isbn { get; set; }
        public int Quantity { get; set; }
        public int Pages { get; set; }
        public int Year { get; set; }
        public string Status { get; set; } = "available";
        public string? Description { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public ICollection<BookReview> Reviews { get; set; } = new List<BookReview>();
    }
}
