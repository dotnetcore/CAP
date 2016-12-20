using Cap.Consistency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace Cap.Consistency.EntityFrameworkCore
{

    public class ConsistencyMessageStore : ConsistencyMessageStore<ConsistencyMessage, DbContext, string>
    {
        public ConsistencyMessageStore(DbContext context) : base(context) { }
    }


    public class ConsistencyMessageStore<TMessage> : ConsistencyMessageStore<TMessage, DbContext, string>
    where TMessage : ConsistencyMessage<string>
    {
        public ConsistencyMessageStore(DbContext context) : base(context) { }
    }


    public class ConsistencyMessageStore<TMessage, TContext> : ConsistencyMessageStore<TMessage, TContext, string>
        where TMessage : ConsistencyMessage<string>
        where TContext : DbContext
    {
        public ConsistencyMessageStore(TContext context) : base(context) { }
    }


    public abstract class ConsistencyMessageStore<TMessage, TContext, TKey> : IConsistencyMessageStore<TMessage>
        where TMessage : ConsistencyMessage<TKey>
        where TContext : DbContext
        where TKey : IEquatable<TKey>
    {

        private bool _disposed;

        public ConsistencyMessageStore(TContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            Context = context;
        }

        public TContext Context { get; private set; }

        private DbSet<TMessage> MessageSet { get { return Context.Set<TMessage>(); } }

        /// <summary>
        /// Creates the specified <paramref name="user"/> in the consistency message store.
        /// </summary>
        /// <param name="message">The message to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="OperateResult"/> of the creation operation.</returns>
        public async virtual Task<OperateResult> CreateAsync(TMessage message, CancellationToken cancellationToken) {
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
        public async virtual Task<OperateResult> DeleteAsync(TMessage message, CancellationToken cancellationToken) {
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
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the user matching the specified <paramref name="messageId"/> if it exists.
        /// </returns>
        public virtual Task<TMessage> FindByIdAsync(string messageId, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var id = ConvertIdFromString(messageId);
            return MessageSet.FindAsync(new object[] { id }, cancellationToken);
        }

        /// <summary>
        /// Converts the provided <paramref name="id"/> to a strongly typed key object.
        /// </summary>
        /// <param name="id">The id to convert.</param>
        /// <returns>An instance of <typeparamref name="TKey"/> representing the provided <paramref name="id"/>.</returns>
        public virtual TKey ConvertIdFromString(string id) {
            if (id == null) {
                return default(TKey);
            }
            return (TKey)TypeDescriptor.GetConverter(typeof(TKey)).ConvertFromInvariantString(id);
        }


        /// <summary>
        /// Converts the provided <paramref name="id"/> to its string representation.
        /// </summary>
        /// <param name="id">The id to convert.</param>
        /// <returns>An <see cref="string"/> representation of the provided <paramref name="id"/>.</returns>
        public virtual string ConvertIdToString(TKey id) {
            if (object.Equals(id, default(TKey))) {
                return null;
            }
            return id.ToString();
        }


        public Task<string> GetMessageIdAsync(TMessage message, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            return Task.FromResult(ConvertIdToString(message.Id));
        }

        public async virtual Task<OperateResult> UpdateAsync(TMessage message, CancellationToken cancellationToken) {
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
