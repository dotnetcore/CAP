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
        public const string ConnectionString = "Server=localhost;Database=testcap;UserId=root;Password=123123;";

        public DbSet<Person> Persons { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            throw new InvalidOperationException("it's can not supported ef core!");
            //optionsBuilder.UseOracle(ConnectionString);
        }
    }
}
