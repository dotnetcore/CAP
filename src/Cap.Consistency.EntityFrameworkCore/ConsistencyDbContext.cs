using System;
using Cap.Consistency.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Cap.Consistency.EntityFrameworkCore
{
    /// <summary>
    /// Base class for the Entity Framework database context used for consistency.
    /// </summary>
    public class ConsistencyDbContext : ConsistencyDbContext<ConsistencyMessage, string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsistencyDbContext"/>.
        /// </summary>
        public ConsistencyDbContext() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsistencyDbContext"/>.
        /// </summary>
        /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
        public ConsistencyDbContext(DbContextOptions options) : base(options) { }
    }

    /// <summary>
    /// Base class for the Entity Framework database context used for consistency.
    /// </summary>
    /// <typeparam name="TMessage">The type of message objects.</typeparam>
    /// <typeparam name="Tkey">The type of the primarky key for messages.</typeparam>
    public abstract class ConsistencyDbContext<TMessage, Tkey> : DbContext
        where TMessage : ConsistencyMessage<Tkey>
        where Tkey : IEquatable<Tkey>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsistencyDbContext"/>.
        /// </summary>
        public ConsistencyDbContext() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsistencyDbContext"/>.
        /// </summary>
        /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
        public ConsistencyDbContext(DbContextOptions options) : base(options) { }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TMessage}"/> of Messages.
        /// </summary>
        public DbSet<TMessage> Messages { get; set; }

        /// <summary>
        /// Configures the schema for the identity framework.
        /// </summary>
        /// <param name="modelBuilder">
        /// The builder being used to construct the model for this context.
        /// </param>
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<TMessage>(b => {
                b.HasKey(m => m.Id);
                b.ToTable("ConsistencyMessages");
            });
        }
    }
}