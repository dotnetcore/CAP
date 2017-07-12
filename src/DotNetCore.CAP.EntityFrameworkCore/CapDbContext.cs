using System.Data.Common;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.CAP.EntityFrameworkCore
{
    /// <summary>
    /// Base class for the Entity Framework database context used for CAP.
    /// </summary>
    public class CapDbContext : DbContext
    {
        private SqlServerOptions _sqlServerOptions;
        /// <summary>
        /// Initializes a new instance of the <see cref="CapDbContext"/>.
        /// </summary>
        public CapDbContext() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CapDbContext"/>.
        /// </summary>
        /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
        public CapDbContext(DbContextOptions<CapDbContext> options, SqlServerOptions sqlServerOptions)
            : base(options) {
            _sqlServerOptions = sqlServerOptions;
        }

        /// <summary>
        /// Gets or sets the <see cref="CapSentMessage"/> of Messages.
        /// </summary>
        public DbSet<CapSentMessage> CapSentMessages { get; set; }
        

        public DbSet<CapQueue> CapQueue { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="CapReceivedMessages"/> of Messages.
        /// </summary>
        public DbSet<CapReceivedMessage> CapReceivedMessages { get; set; }

        public DbConnection GetDbConnection() => Database.GetDbConnection();

        /// <summary>
        /// Configures the schema for the identity framework.
        /// </summary>
        /// <param name="modelBuilder">
        /// The builder being used to construct the model for this context.
        /// </param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(_sqlServerOptions.Schema);

            modelBuilder.Entity<CapSentMessage>(b =>
            {
                b.HasKey(m => m.Id);
                b.HasIndex(x => x.StatusName);
                b.Property(p => p.StatusName).IsRequired().HasMaxLength(50);
            });

            modelBuilder.Entity<CapReceivedMessage>(b =>
            {
                b.HasKey(m => m.Id);
                b.HasIndex(x => x.StatusName);
                b.Property(p => p.StatusName).IsRequired().HasMaxLength(50);
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }
    }
}