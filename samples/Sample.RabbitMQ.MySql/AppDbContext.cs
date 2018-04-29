using Microsoft.EntityFrameworkCore;

namespace Sample.RabbitMQ.MySql
{
    public class AppDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("Server=192.168.10.110;Database=testcap;UserId=root;Password=123123;");
        }
    }
}
