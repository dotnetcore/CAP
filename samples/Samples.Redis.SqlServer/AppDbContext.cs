using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Sample.Redis.SqlServer
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> optionsBuilder) : base(optionsBuilder)
        {

        }

    }
}
