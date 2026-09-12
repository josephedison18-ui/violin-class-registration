using ViolinClassAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ViolinClassAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Registration> Registrations { get; set; }
        public DbSet<PageVisitor> PageVisitors { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<SmsNotification> SmsNotifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Registration Table Configuration
            modelBuilder.Entity<Registration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(15);
                entity.Property(e => e.Gender).HasMaxLength(20);
                entity.Property(e => e.City).HasMaxLength(50);
                entity.Property(e => e.Country).HasMaxLength(50);
                entity.Property(e => e.ClassMode).HasMaxLength(20); // online, offline
                entity.Property(e => e.PaymentStatus).HasMaxLength(20); // pending, completed
                entity.Property(e => e.Status).HasMaxLength(20); // active, inactive
                entity.HasIndex(e => e.Phone).IsUnique();
                entity.HasIndex(e => e.Email);
            });

            // Page Visitor Table
            modelBuilder.Entity<PageVisitor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.IpAddress).HasMaxLength(50);
            });

            // Admin User Table
            modelBuilder.Entity<AdminUser>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Password).IsRequired();
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // SMS Notification Table
            modelBuilder.Entity<SmsNotification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(15);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.Status).HasMaxLength(20); // sent, pending, failed
            });
        }
    }
}
