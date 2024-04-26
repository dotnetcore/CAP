using Microsoft.EntityFrameworkCore;

namespace Sample.RabbitMQ.SqlServer
{
    public class Person
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }

        public override string ToString()
        {
            return $"Name:{Name}, Age:{Age}";
        }
    }

    public class AppDbContext : DbContext
    {
        public const string ConnectionString = "Server=127.0.0.1;Database=tempdb;User Id=sa;Password=yourStrong(!)Password;TrustServerCertificate=True";

        public DbSet<Person> Persons { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(ConnectionString);
        }
    }
}
