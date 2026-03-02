using System.ComponentModel.DataAnnotations;

namespace Library_Management_system.Models
{
    public class FavoriteBook
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(200)]
        public string OwnerKey { get; set; } = string.Empty;

        public int BookId { get; set; }
        public Book? Book { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
