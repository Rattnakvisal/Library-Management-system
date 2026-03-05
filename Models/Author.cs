using System.ComponentModel.DataAnnotations;

namespace Library_Management_system.Models
{
    public class Author
    {
        [Key]
        public int AuthorID { get; set; }

        [Required]
        [MaxLength(100)]
        public string AuthorName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }
    }
}
