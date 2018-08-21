using Microsoft.EntityFrameworkCore;

namespace Sample.RabbitMQ.MySql
{
    public class Person
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public const string ConnectionString = "Server=192.168.10.110;Database=testcap;UserId=root;Password=123123;IgnoreCommandTransaction=true;";

        public DbSet<Person> Persons { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(ConnectionString);
        }
    }
}
