using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cap.Consistency
{
    /// <summary>
    /// Provides an abstraction for a store which manages kafka consistent message.
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IConsistentMessageStore<TMessage> : IDisposable where TMessage : class
    {
        /// <summary>
        /// Finds and returns a message, if any, who has the specified <paramref name="messageId"/>.
        /// </summary>
        /// <param name="messageId">The message ID to search for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the message matching the specified <paramref name="messageId"/> if it exists.
        /// </returns>
        Task<TMessage> FindByIdAsync(string messageId, CancellationToken cancellationToken);

        /// <summary>
        ///  Creates a new message in a store as an asynchronous operation.
        /// </summary>
        /// <param name="message">The message to create in the store.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the <see cref="IdentityResult"/> of the asynchronous query.</returns>
        Task<OperateResult> CreateAsync(TMessage message, CancellationToken cancellationToken);

        /// <summary>
        /// Updates a message in a store as an asynchronous operation.
        /// </summary>
        /// <param name="message">The message to update in the store.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the <see cref="IdentityResult"/> of the asynchronous query.</returns>
        Task<OperateResult> UpdateAsync(TMessage message, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a message from the store as an asynchronous operation.
        /// </summary>
        /// <param name="message">The message to delete in the store.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the <see cref="IdentityResult"/> of the asynchronous query.</returns>
        Task<OperateResult> DeleteAsync(TMessage message, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the ID for a message from the store as an asynchronous operation.
        /// </summary>
        /// <param name="message">The message whose ID should be returned.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains the ID of the message.</returns>
        Task<string> GetMessageIdAsync(TMessage message, CancellationToken cancellationToken);
    }
}