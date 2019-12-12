using Microsoft.EntityFrameworkCore;

namespace Sample.RabbitMQ.SqlServer
{
    public class Person
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            return $"Name:{Name}, Id:{Id}";
        }
    }

    public class Person2
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            return $"Name:{Name}, Id:{Id}";
        }
    }

    public class AppDbContext : DbContext
    {
        public const string ConnectionString = "Server=192.168.2.120;Database=captest;User Id=sa;Password=P@ssw0rd;";

        public DbSet<Person> Persons { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(ConnectionString);
        }
    }
}
