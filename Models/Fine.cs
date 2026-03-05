using System.ComponentModel.DataAnnotations;

namespace Library_Management_system.Models
{
    public class Fine
    {
        [Key]
        public int FineID { get; set; }

        public int BorrowID { get; set; }
        public BorrowingRecord? Borrowing { get; set; }

        public decimal Amount { get; set; }
        public bool Paid { get; set; }
        public DateTime? PaidDate { get; set; }

        [MaxLength(1000)]
        public string? Remark { get; set; }
    }
}
