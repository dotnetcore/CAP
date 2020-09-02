using Microsoft.EntityFrameworkCore;
using System;

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
        public const string ConnectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=xe)));User Id=test;Password=test;Persist Security Info=True;";
        public DbSet<Person> Persons { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseOracle(ConnectionString);
        }
    }
}
