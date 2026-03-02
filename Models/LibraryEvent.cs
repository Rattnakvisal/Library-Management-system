using System.ComponentModel.DataAnnotations;

namespace Library_Management_system.Models
{
    public class LibraryEvent
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(160)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(3000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [MaxLength(400)]
        public string? ImageUrl { get; set; }

        [MaxLength(150)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
