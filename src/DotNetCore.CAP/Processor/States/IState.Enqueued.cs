using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Processor.States
{
    public class EnqueuedState : IState
    {
        public const string StateName = "Enqueued";

        public TimeSpan? ExpiresAfter => null;

        public string Name => StateName;

        public void Apply(CapPublishedMessage message, IStorageTransaction transaction)
        {
            transaction.EnqueueMessage(message);
        }

        public void Apply(CapReceivedMessage message, IStorageTransaction transaction)
        {
            transaction.EnqueueMessage(message);
        }
    }
}