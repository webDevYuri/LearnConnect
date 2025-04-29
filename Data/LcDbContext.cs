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
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostLike> PostLikes { get; set; }
        public DbSet<PostComment> PostComments { get; set; }

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

            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.MediaPath).HasMaxLength(255);
                entity.Property(e => e.MediaType).HasMaxLength(10);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UserProfileId).IsRequired();

                entity.HasOne(e => e.UserProfile)
                    .WithMany()
                    .HasForeignKey(e => e.UserProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PostLike>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PostId).IsRequired();
                entity.Property(e => e.UserProfileId).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasOne(e => e.Post)
                    .WithMany(p => p.Likes)
                    .HasForeignKey(e => e.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.UserProfile)
                    .WithMany()
                    .HasForeignKey(e => e.UserProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PostComment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.PostId).IsRequired();
                entity.Property(e => e.UserProfileId).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasOne(e => e.Post)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(e => e.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.UserProfile)
                    .WithMany()
                    .HasForeignKey(e => e.UserProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}