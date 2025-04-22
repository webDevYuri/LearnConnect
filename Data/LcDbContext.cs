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

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        
        }
    }
}