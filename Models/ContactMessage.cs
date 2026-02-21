using System.ComponentModel.DataAnnotations;

namespace Library_Management_system.Models
{
    public class ContactMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
