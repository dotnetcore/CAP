using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.EntityFrameworkCore;
using DotNetCore.CAP.Infrastructure;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Sample.Kafka
{
    public class AppDbContext : CapDbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=DESKTOP-M9R8T31;Initial Catalog=WebApp1;User Id=sa;Password=P@ssw0rd;MultipleActiveResultSets=True");
        }
    }
}
