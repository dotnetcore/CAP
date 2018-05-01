using Microsoft.EntityFrameworkCore;

namespace SkyWalking.Diagnostics.EntityFrameworkCore.Tests.Fakes
{
    public class FakeDbContext : DbContext
    {
        public FakeDbContext(DbContextOptions<FakeDbContext> options)
            : base(options)
        {
        }

        public DbSet<FakeUser> Users { get; set; }
    }
}
