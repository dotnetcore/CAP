using Microsoft.EntityFrameworkCore;

namespace Sample.RabbitMQ.Oracle
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

        public const string ConnectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.0.2)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORCL)));User Id=sa;Password=P@ssw0rd;";
        public DbSet<Person> Persons { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseOracle(ConnectionString);
        }
    }
}
