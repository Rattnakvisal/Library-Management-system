using System.ComponentModel;
using Library_Management_system.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_system.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Book> Books { get; set; }
        public DbSet<LibraryEvent> Events { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<FavoriteBook> FavoriteBooks { get; set; }
        public DbSet<BorrowingRecord> BorrowingRecords { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<BookReview> BookReviews { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<FavoriteBook>(entity =>
            {
                entity.HasIndex(x => new { x.OwnerKey, x.BookId }).IsUnique();
                entity.Property(x => x.OwnerKey).HasMaxLength(200);
            });

            builder.Entity<BookReview>(entity =>
            {
                entity.HasIndex(x => new { x.BookId, x.UserId }).IsUnique();
                entity.Property(x => x.UserId).HasMaxLength(450);
                entity.Property(x => x.Username).HasMaxLength(150);
                entity.Property(x => x.Email).HasMaxLength(256);

                entity.HasOne(x => x.Book)
                    .WithMany(x => x.Reviews)
                    .HasForeignKey(x => x.BookId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
