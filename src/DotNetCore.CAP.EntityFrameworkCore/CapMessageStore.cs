using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Models;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.CAP.EntityFrameworkCore
{
    /// <summary>
    /// Represents a new instance of a persistence store for the specified message types.
    /// </summary>
    /// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
    public class CapMessageStore<TContext> : ICapMessageStore where TContext : DbContext
    {
        /// <summary>
        /// Constructs a new instance of <see cref="TContext"/>.
        /// </summary>
        /// <param name="context">The <see cref="DbContext"/>.</param>
        public CapMessageStore(TContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public TContext Context { get; private set; }

        private DbSet<CapSentMessage> SentMessages => Context.Set<CapSentMessage>();

        /// <summary>
        /// Creates the specified <paramref name="message"/> in the cap message store.
        /// </summary>
        /// <param name="message">The message to create.</param>
        public async Task<OperateResult> StoreSentMessageAsync(CapSentMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            Context.Add(message);
            await Context.SaveChangesAsync();
            return OperateResult.Success;
        }
    }
}