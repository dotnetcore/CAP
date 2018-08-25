using Microsoft.EntityFrameworkCore;

namespace Sample.Kafka.SqlServer
{
    public class Person
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public const string ConnectionString = "Server=localhost;Integrated Security=SSPI;Database=testcap";

        public DbSet<Person> Persons { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(ConnectionString);
        }
    }
}
