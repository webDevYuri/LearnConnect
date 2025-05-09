﻿using Microsoft.EntityFrameworkCore;
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
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<AnswerUpvote> AnswerUpvotes { get; set; }

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

            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Details).IsRequired();
                entity.Property(e => e.Tags).HasMaxLength(200);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UserProfileId).IsRequired();

                entity.HasOne(e => e.UserProfile)
                    .WithMany()
                    .HasForeignKey(e => e.UserProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Answer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.QuestionId).IsRequired();
                entity.Property(e => e.UserProfileId).IsRequired();

                entity.HasOne(e => e.Question)
                    .WithMany(q => q.Answers)
                    .HasForeignKey(e => e.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.UserProfile)
                    .WithMany()
                    .HasForeignKey(e => e.UserProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AnswerUpvote>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AnswerId).IsRequired();
                entity.Property(e => e.UserProfileId).IsRequired();

                entity.HasOne(e => e.Answer)
                    .WithMany(a => a.Upvotes)
                    .HasForeignKey(e => e.AnswerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.UserProfile)
                    .WithMany()
                    .HasForeignKey(e => e.UserProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}