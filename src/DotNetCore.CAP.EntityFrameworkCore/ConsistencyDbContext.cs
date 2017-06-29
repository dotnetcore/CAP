using DotNetCore.CAP.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.CAP.EntityFrameworkCore
{
    /// <summary>
    /// Base class for the Entity Framework database context used for CAP.
    /// </summary>
    /// <typeparam name="TMessage">The type of message objects.</typeparam>
    /// <typeparam name="Tkey">The type of the primarky key for messages.</typeparam>
    public class CapDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CapDbContext"/>.
        /// </summary>
        public CapDbContext() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CapDbContext"/>.
        /// </summary>
        /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
        public CapDbContext(DbContextOptions options) : base(options) { }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{ConsistencyMessage}"/> of Messages.
        /// </summary>
        public DbSet<CapSentMessage> CapSentMessages { get; set; }

        public DbSet<CapReceivedMessage> CapReceivedMessages { get; set; }

        /// <summary>
        /// Configures the schema for the identity framework.
        /// </summary>
        /// <param name="modelBuilder">
        /// The builder being used to construct the model for this context.
        /// </param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CapSentMessage>(b =>
            {
                b.HasKey(m => m.Id);
                b.Property(p => p.StatusName).HasMaxLength(50);
            });

            modelBuilder.Entity<CapReceivedMessage>(b =>
            {
                b.Property(p => p.StatusName).HasMaxLength(50);
            });
        }
    }
}