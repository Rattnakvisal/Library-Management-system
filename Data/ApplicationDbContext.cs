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
        public DbSet<Author> Authors { get; set; }
        public DbSet<Fine> Fines { get; set; }

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

            builder.Entity<Author>(entity =>
            {
                entity.HasKey(x => x.AuthorID);
                entity.Property(x => x.AuthorID).HasColumnName("Id");
                entity.Property(x => x.AuthorName).HasColumnName("Name").HasMaxLength(100);
                entity.Property(x => x.CreatedBy).HasMaxLength(150);
                entity.Property(x => x.CreatedDate);
                entity.HasIndex(x => x.AuthorName).IsUnique();
            });

            builder.Entity<Book>(entity =>
            {
                entity.Property(x => x.Title).HasMaxLength(200);
                entity.Property(x => x.BookCode).HasMaxLength(50);
                entity.Property(x => x.BookImage).HasMaxLength(255);
                entity.Property(x => x.Summarized).HasMaxLength(500);

                entity.HasOne(x => x.Category)
                    .WithMany()
                    .HasForeignKey(x => x.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.AuthorEntity)
                    .WithMany()
                    .HasForeignKey(x => x.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<BorrowingRecord>(entity =>
            {
                entity.ToTable("Borrowing");
                entity.Property(x => x.DurationDays).HasDefaultValue(14);
                entity.Property(x => x.ReturnUserId).HasMaxLength(450);
                entity.Property(x => x.Reason).HasMaxLength(100);

                entity.HasOne(x => x.Reservation)
                    .WithMany()
                    .HasForeignKey(x => x.ReservationId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            builder.Entity<Fine>(entity =>
            {
                entity.ToTable("fines");
                entity.HasKey(x => x.FineID);
                entity.Property(x => x.FineID).ValueGeneratedOnAdd();
                entity.Property(x => x.Amount).HasColumnType("decimal(10,2)");
                entity.Property(x => x.Paid).HasDefaultValue(false);
                entity.Property(x => x.Remark).HasMaxLength(1000);

                entity.HasIndex(x => x.BorrowID).IsUnique();

                entity.HasOne(x => x.Borrowing)
                    .WithOne(x => x.Fine)
                    .HasForeignKey<Fine>(x => x.BorrowID)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
