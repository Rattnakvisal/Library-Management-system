using System.ComponentModel.DataAnnotations;

namespace Library_Management_system.Models
{
    public class BorrowingRecord
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(150)]
        public string Username { get; set; } = string.Empty;

        public int BookId { get; set; }
        public Book? Book { get; set; }

        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }

        [MaxLength(30)]
        public string Status { get; set; } = "active";

        [MaxLength(30)]
        public string Source { get; set; } = "in_person";

        [MaxLength(150)]
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
