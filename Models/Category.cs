using System.ComponentModel.DataAnnotations;

namespace Library_Management_system.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
