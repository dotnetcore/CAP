using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Processor.States
{
    public interface IState
    {
        TimeSpan? ExpiresAfter { get; }

        string Name { get; }

        void Apply(CapSentMessage message, IStorageTransaction transaction);

        void Apply(CapReceivedMessage message, IStorageTransaction transaction);
    }
}