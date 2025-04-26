using Microsoft.EntityFrameworkCore;
using LearnConnect.Models;

namespace LearnConnect.Data
{ 
    public partial class LcDbContext : DbContext
    {
        public LcDbContext()
        {
        }

        public LcDbContext(DbContextOptions<LcDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.FirstName).IsRequired();
                entity.Property(e => e.LastName).IsRequired();
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Gender).HasMaxLength(10);
                entity.Property(e => e.ProfilePhotoPath).HasMaxLength(255);
                
                entity.Ignore(e => e.ProfilePhoto);
            });
        }
    }
}