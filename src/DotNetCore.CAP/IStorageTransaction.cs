using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    /// <summary>
    /// A transactional database storage operation.
    /// Update message state of the message table with transactional.
    /// </summary>
    public interface IStorageTransaction : IDisposable
    {
        void UpdateMessage(CapPublishedMessage message);

        void UpdateMessage(CapReceivedMessage message);

        void EnqueueMessage(CapPublishedMessage message);

        void EnqueueMessage(CapReceivedMessage message);

        Task CommitAsync();
    }
}