using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Library_Management_system.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }
    }
}
