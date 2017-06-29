using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Sample.Kafka
{
    public class AppDbContext : DbContext
    {

        public DbSet<CapSentMessage> SentMessages { get; set; }

        public DbSet<CapReceivedMessage> ReceivedMessages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=192.168.2.206;Initial Catalog=Test;User Id=cmswuliu;Password=h7xY81agBn*Veiu3;MultipleActiveResultSets=True");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CapSentMessage>().Property(x => x.StatusName).HasMaxLength(50);
            modelBuilder.Entity<CapReceivedMessage>().Property(x => x.StatusName).HasMaxLength(50);

            base.OnModelCreating(modelBuilder);
        }
    }
}
