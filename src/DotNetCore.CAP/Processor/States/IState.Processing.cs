using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Processor.States
{
    public class ProcessingState : IState
    {
        public const string StateName = "Processing";

        public TimeSpan? ExpiresAfter => null;

        public string Name => StateName;

        public void Apply(CapPublishedMessage message, IStorageTransaction transaction)
        {
        }

        public void Apply(CapReceivedMessage message, IStorageTransaction transaction)
        {
        }
    }
}