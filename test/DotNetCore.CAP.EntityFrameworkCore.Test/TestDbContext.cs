using Microsoft.EntityFrameworkCore;

namespace DotNetCore.CAP.EntityFrameworkCore.Test
{
    public class TestDbContext : CapDbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = ConnectionUtil.GetConnectionString();
            optionsBuilder.UseSqlServer(connectionString);
        }
    }
}