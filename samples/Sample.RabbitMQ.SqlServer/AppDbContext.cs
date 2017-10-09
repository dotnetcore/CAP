using Microsoft.EntityFrameworkCore;
using Sample.RabbitMQ.SqlServer.Controllers;

namespace Sample.RabbitMQ.SqlServer
{
    public class AppDbContext : DbContext
    {

        public DbSet<Person> Persons { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
           //optionsBuilder.UseSqlServer("Server=192.168.2.206;Initial Catalog=TestCap;User Id=cmswuliu;Password=h7xY81agBn*Veiu3;MultipleActiveResultSets=True");
            optionsBuilder.UseSqlServer("Server=DESKTOP-M9R8T31;Initial Catalog=Sample.Kafka.SqlServer;User Id=sa;Password=P@ssw0rd;MultipleActiveResultSets=True");
        }
    }
}
