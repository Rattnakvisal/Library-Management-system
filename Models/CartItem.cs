using System.ComponentModel.DataAnnotations;

namespace Library_Management_system.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(150)]
        public string OwnerKey { get; set; } = string.Empty;

        public int BookId { get; set; }
        public Book? Book { get; set; }

        public DateTime CreatedDate { get; set; }
        public bool IsRequested { get; set; }
        public DateTime? RequestedDate { get; set; }
        [MaxLength(30)]
        public string ReservationStatus { get; set; } = "none";
        public DateTime? ReservationUpdatedDate { get; set; }
        public bool IsReservationNotificationSeen { get; set; }
    }
}
