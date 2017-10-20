using Microsoft.EntityFrameworkCore;

namespace Sample.Kafka.SqlServer
{
    public class AppDbContext : DbContext
    {

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
           //optionsBuilder.UseSqlServer("Server=192.168.2.206;Initial Catalog=Sample.Kafka.SqlServer;User Id=cmswuliu;Password=h7xY81agBn*Veiu3;MultipleActiveResultSets=True");
           optionsBuilder.UseSqlServer("Server=192.168.2.206;Initial Catalog=TestCap;User Id=cmswuliu;Password=h7xY81agBn*Veiu3;MultipleActiveResultSets=True");
        }
    }
}
