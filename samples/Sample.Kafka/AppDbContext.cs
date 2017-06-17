using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cap.Consistency.Infrastructure;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Sample.Kafka
{
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions options) : base(options) {
        }

        public DbSet<ConsistencyMessage> Messages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            //optionsBuilder.UseSqlServer
            base.OnConfiguring(optionsBuilder);
        }

    }
}
