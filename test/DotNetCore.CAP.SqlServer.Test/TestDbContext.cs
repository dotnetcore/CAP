using Microsoft.EntityFrameworkCore;

namespace DotNetCore.CAP.SqlServer.Test
{
    public class TestDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = ConnectionUtil.GetConnectionString();
            optionsBuilder.UseSqlServer(connectionString);
        }
    }
}