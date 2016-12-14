using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cap.Consistency
{
    /// <summary>
    /// Provides the APIs for managing message in a persistence store.
    /// </summary>
    /// <typeparam name="TMessage">The type encapsulating a message.</typeparam>
    public class ConsistentMessageManager<TMessage> : IDisposable where TMessage : class
    {
        private bool _disposed;
        private readonly HttpContext _context;
        private CancellationToken CancellationToken => _context?.RequestAborted ?? CancellationToken.None;

        /// <summary>
        /// Constructs a new instance of <see cref="ConsistentMessageManager{TMessage}"/>.
        /// </summary>
        /// <param name="store">The persistence store the manager will operate over.</param>
        /// <param name="services">The <see cref="IServiceProvider"/> used to resolve services.</param>
        /// <param name="logger">The logger used to log messages, warnings and errors.</param>
        public ConsistentMessageManager(IConsistentMessageStore<TMessage> store,
            IServiceProvider services,
            ILogger<ConsistentMessageManager<TMessage>> logger) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            Store = store;
            Logger = logger;

            if (services != null) {
                _context = services.GetService<IHttpContextAccessor>()?.HttpContext;
            }
        }

        /// <summary>
        ///  Gets or sets the persistence store the manager operates over.
        /// </summary>
        /// <value>The persistence store the manager operates over.</value>
        protected internal IConsistentMessageStore<TMessage> Store { get; set; }

        /// <summary>
        /// Gets the <see cref="ILogger"/> used to log messages from the manager.
        /// </summary>
        /// <value>
        /// The <see cref="ILogger"/> used to log messages from the manager.
        /// </value>
        protected internal virtual ILogger Logger { get; set; }

        /// <summary>
        ///  Creates the specified <paramref name="message"/> in the backing store.
        /// </summary>
        /// <param name="message">The message to create.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="OperateResult"/>
        /// of the operation.
        /// </returns>
        public virtual Task<OperateResult> CreateAsync(TMessage message) {
            ThrowIfDisposed();
            //todo: validation message fileds is correct

            return Store.CreateAsync(message, CancellationToken);
        }

        /// <summary>
        /// Updates the specified <paramref name="message"/> in the backing store.
        /// </summary>
        /// <param name="message">The message to update.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="OperateResult"/>
        /// of the operation.
        /// </returns>
        public virtual Task<OperateResult> UpdateMessageAsync(TMessage message) {
            ThrowIfDisposed();
            //todo: validation message fileds is correct

            return Store.UpdateAsync(message, CancellationToken);
        }

        /// <summary>
        /// Deletes the specified <paramref name="message"/> in the backing store.
        /// </summary>
        /// <param name="message">The message to delete.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="OperateResult"/>
        /// of the operation.
        /// </returns>
        public virtual Task<OperateResult> DeleteMessageAsync(TMessage message) {
            ThrowIfDisposed();

            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }

            return Store.DeleteAsync(message, CancellationToken);
        }

        /// <summary>
        /// Finds and returns a message, if any, who has the specified <paramref name="messageId"/>.
        /// </summary>
        /// <param name="messageId">The message ID to search for.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the user matching the specified <paramref name="messageId"/> if it exists.
        /// </returns>
        public virtual Task<TMessage> FindByIdAsync(string messageId) {
            ThrowIfDisposed();
            return Store.FindByIdAsync(messageId, CancellationToken);
        }

        /// <summary>
        /// Gets the message identifier for the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message whose identifier should be retrieved.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the identifier for the specified <paramref name="message"/>.</returns>
        public virtual async Task<string> GetMessageIdAsync(TMessage message) {
            ThrowIfDisposed();
            return await Store.GetMessageIdAsync(message, CancellationToken);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the message manager and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing) {
            if (disposing && !_disposed) {
                Store.Dispose();
                _disposed = true;
            }
        }

        protected void ThrowIfDisposed() {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}