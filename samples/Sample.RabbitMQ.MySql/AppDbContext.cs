using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Sample.RabbitMQ.MySql
{
    public class AppDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("Server=localhost;Database=Sample.RabbitMQ.MySql;UserId=root;Password=123123;Allow User Variables=True");
            //optionsBuilder.UseMySql("Server=192.168.2.206;Database=Sample.RabbitMQ.MySql;UserId=root;Password=123123;");
        }
    }
}
