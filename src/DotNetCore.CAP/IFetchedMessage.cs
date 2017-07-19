using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    public interface IFetchedMessage : IDisposable
    {
        int MessageId { get; }

        MessageType MessageType { get; }

        void RemoveFromQueue();

        void Requeue();
    }
}