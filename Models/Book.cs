using System.ComponentModel.DataAnnotations;

namespace Library_Management_system.Models
{
    public class Book
    {
        [Key]
        public int Id { get; set; }

        // Schema-aligned fields (books table)
        [MaxLength(50)]
        public string BookCode { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? BookImage { get; set; }

        [MaxLength(500)]
        public string? Summarized { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int AuthorId { get; set; }
        public Author? AuthorEntity { get; set; }

        public string Author { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? Isbn { get; set; }
        public int Quantity { get; set; }
        public bool Availability { get; set; } = true;
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
