using Microsoft.EntityFrameworkCore;

namespace SkyApm.Sample.Backend.Models
{
    public class SampleDbContext :DbContext
    {
        public  DbSet<Application> Applications { get; set; }

        public SampleDbContext(DbContextOptions options):base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Application>().HasKey(x => x.Id);
            modelBuilder.Entity<Application>().Property(x => x.Name);
            base.OnModelCreating(modelBuilder);
        }
    }
}