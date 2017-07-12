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
    public class AppDbContext :DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=192.168.2.206;Initial Catalog=Test;User Id=cmswuliu;Password=h7xY81agBn*Veiu3;MultipleActiveResultSets=True");
        }
    }
}
