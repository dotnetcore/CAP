using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cap.Consistency.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Cap.Consistency.EntityFrameworkCore
{
    /// <summary>
    /// Represents a new instance of a persistence store for the specified message types.
    /// </summary>
    /// <typeparam name="ConsistencyMessage">The type representing a message.</typeparam>
    /// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
    /// <typeparam name="TKey">The type of the primary key for a message.</typeparam>
    public class ConsistencyMessageStore<TContext> : IConsistencyMessageStore where TContext : DbContext
    {
        private bool _disposed;

        /// <summary>
        /// Constructs a new instance of <see cref="ConsistencyMessageStore{ConsistencyMessage, TContext, TKey}"/>.
        /// </summary>
        /// <param name="context">The <see cref="DbContext"/>.</param>
        public ConsistencyMessageStore(TContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            Context = context;
        }

        public TContext Context { get; private set; }

        private DbSet<ConsistencyMessage> MessageSet { get { return Context.Set<ConsistencyMessage>(); } }

        /// <summary>
        /// Creates the specified <paramref name="message"/> in the consistency message store.
        /// </summary>
        /// <param name="message">The message to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="OperateResult"/> of the creation operation.</returns>
        public async virtual Task<OperateResult> CreateAsync(ConsistencyMessage message, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            Context.Add(message);
            await SaveChanges(cancellationToken);
            return OperateResult.Success;
        }

        /// <summary>
        /// Deletes the specified <paramref name="message"/> from the consistency message store.
        /// </summary>
        /// <param name="message">The message to delete.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="OperateResult"/> of the update operation.</returns>
        public async virtual Task<OperateResult> DeleteAsync(ConsistencyMessage message, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }

            Context.Remove(message);
            try {
                await SaveChanges(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex) {
                return OperateResult.Failed(new OperateError() { Code = "DbUpdateConcurrencyException", Description = ex.Message });
            }
            return OperateResult.Success;
        }

        /// <summary>
        /// Finds and returns a message, if any, who has the specified <paramref name="messageId"/>.
        /// </summary>
        /// <param name="messageId">The message ID to search for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the message matching the specified <paramref name="messageId"/> if it exists.
        /// </returns>
        public virtual Task<ConsistencyMessage> FindByIdAsync(string messageId, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return MessageSet.FindAsync(new object[] { messageId }, cancellationToken);
        }

        /// <summary>
        /// Gets the message identifier for the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message whose identifier should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the identifier for the specified <paramref name="message"/>.</returns>
        public Task<string> GeConsistencyMessageIdAsync(ConsistencyMessage message, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            return Task.FromResult(message.Id);
        }

        /// <summary>
        /// Updates the specified <paramref name="message"/> in the message store.
        /// </summary>
        /// <param name="message">The message to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="OperateResult"/> of the update operation.</returns>
        public async virtual Task<OperateResult> UpdateAsync(ConsistencyMessage message, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }

            Context.Attach(message);
            message.UpdateTime = DateTime.Now;
            Context.Update(message);
            try {
                await SaveChanges(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex) {
                return OperateResult.Failed(new OperateError() { Code = "DbUpdateConcurrencyException", Description = ex.Message });
            }
            return OperateResult.Success;
        }

        public Task<ConsistencyMessage> GetFirstEnqueuedMessageAsync(CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            return MessageSet.AsNoTracking().Where(x => x.Status == MessageStatus.WaitForSend).FirstOrDefaultAsync(cancellationToken);
        }

        //public void ChangeState(ConsistencyMessage message, MessageStatus status) {
        //    Context.Attach(message);
        //    message.Status = status;
        //    Context.Update(message);
        //    try {
        //        await SaveChanges(cancellationToken);
        //    }
        //    catch (DbUpdateConcurrencyException ex) {
        //        return OperateResult.Failed(new OperateError() { Code = "DbUpdateConcurrencyException", Description = ex.Message });
        //    }
        //    return OperateResult.Success;
        //}

        /// <summary>
        /// Gets or sets a flag indicating if changes should be persisted after CreateAsync, UpdateAsync and DeleteAsync are called.
        /// </summary>
        /// <value>
        /// True if changes should be automatically persisted, otherwise false.
        /// </value>
        public bool AutoSaveChanges { get; set; } = true;

        /// <summary>Saves the current store.</summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        protected Task SaveChanges(CancellationToken cancellationToken) {
            return AutoSaveChanges ? Context.SaveChangesAsync(cancellationToken) : Task.CompletedTask;
        }

        /// <summary>
        /// Throws if this class has been disposed.
        /// </summary>
        protected void ThrowIfDisposed() {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        /// Dispose the store
        /// </summary>
        public void Dispose() {
            _disposed = true;
        }
    }
}